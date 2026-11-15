"""Real HTTP/student verification in a new disposable PostgreSQL database.
Requires private PGHOST/PGPORT/PGUSER/PGPASSWORD and LAB5_STUDENT_VERIFY_CONNECTION.
The connection setting must already point to the new database name passed in --database.
"""
import argparse
import datetime
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.request

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--database", required=True)
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()
if not re.fullmatch(r"equipment_borrowing_verify_lab5_[a-z0-9_]{1,30}", args.database):
    parser.error("A uniquely named Lab 5 verification database is required.")
root = Path(__file__).resolve().parents[1]
psql = os.environ.get("PSQL_PATH", r"C:\Program Files\PostgreSQL\18\bin\psql.exe")
connection = os.environ["LAB5_STUDENT_VERIFY_CONNECTION"]
target = re.search(r'(?:^|;)database=([^;]+)', connection, re.IGNORECASE)
if target is None or target.group(1).strip('"') != args.database:
    raise SystemExit("The private API connection must target the new verification database.")
trace = []

def sql(statement, database=None):
    command = [psql, "-X", "-w", "-At", "-v", "ON_ERROR_STOP=1", "-d", database or args.database]
    result = subprocess.run(command, input=statement, text=True, capture_output=True)
    if result.returncode:
        raise RuntimeError("Verification SQL failed; credentials and SQL error detail omitted.")
    return result.stdout.strip()

def snapshot():
    return json.loads(sql("""SELECT json_build_object(
      'students',(SELECT json_agg(t ORDER BY t."Id") FROM "Students" t),
      'equipment',(SELECT json_agg(t ORDER BY t."Id") FROM "Equipment" t),
      'borrowings',(SELECT json_agg(t ORDER BY t."Id") FROM "Borrowings" t));"""))

sql(f'CREATE DATABASE "{args.database}";', "postgres")
env = dict(os.environ)
env["EQUIPMENT_BORROWING_CONNECTION_STRING"] = connection
env["ASPNETCORE_ENVIRONMENT"] = "Production"
# Never print migration failure details, since connection exceptions may contain private values.
migrate = subprocess.run(["dotnet", "ef", "database", "update", "--project", "src/EquipmentBorrowing.Infrastructure",
    "--startup-project", "src/EquipmentBorrowing.Infrastructure", "--configuration", "Release", "--no-build"],
    cwd=root, env=env, capture_output=True, text=True)
if migrate.returncode:
    raise RuntimeError("Verification migration failed; private output omitted.")
sql("""INSERT INTO "Students" ("StudentNumber","FullName","IsEligibleToBorrow") VALUES
 ('ST-100','Ada Cruz',true),('ST-200','Ben Diaz',false),('ST-300','Cara Lim',true),('ST-400','Dani Ong',true);
 INSERT INTO "Equipment" ("EquipmentIdentifier","Name","IsAvailable") VALUES
 ('EQ-001','Camera',false),('EQ-002','Laptop',true),('EQ-003','Projector',true),
 ('EQ-004','Tablet',false),('EQ-005','Microphone',false),('EQ-006','Tripod',true),('EQ-007','Speaker',true);""")
today = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=8))).date()
for student, item, offset, due, returned in [('ST-100','EQ-001',-9,-1,None),('ST-100','EQ-003',-8,2,-2),
                                           ('ST-300','EQ-004',-7,2,None),('ST-300','EQ-005',-7,2,None)]:
    start, expected = today+datetime.timedelta(days=offset), today+datetime.timedelta(days=due)
    actual = 'NULL' if returned is None else "'"+str(today+datetime.timedelta(days=returned))+"'"
    status = 'Active' if returned is None else 'Returned'
    sql(f"""INSERT INTO "Borrowings" ("StudentId","EquipmentId","BorrowedAt","ExpectedReturnAt","ActualReturnAt","Status")
      SELECT s."Id",e."Id",'{start}','{expected}',{actual},'{status}' FROM "Students" s,"Equipment" e
      WHERE s."StudentNumber"='{student}' AND e."EquipmentIdentifier"='{item}';""")
with socket.socket() as sock:
    sock.bind(("127.0.0.1",0));port=sock.getsockname()[1]
base=f"http://127.0.0.1:{port}"
log=tempfile.TemporaryFile()
process=None

def start():
    global process
    dll=root/'src/EquipmentBorrowing.Api/bin/Release/net10.0/EquipmentBorrowing.Api.dll'
    process=subprocess.Popen(['dotnet',str(dll),'--urls',base],cwd=dll.parent,env=env,stdout=log,stderr=log)
    for _ in range(80):
        try:
            with urllib.request.urlopen(base+'/api/v1/status',timeout=1) as response:
                if response.status==200:return
        except (urllib.error.URLError,TimeoutError):time.sleep(.25)
    raise RuntimeError('API startup timeout; private logs omitted.')

def stop():
    if process and process.poll() is None:
        process.terminate()
        try:process.wait(timeout=5)
        except subprocess.TimeoutExpired:process.kill();process.wait()

def call(method,path,expected,body=None,raw=None,check_state=True):
    before=snapshot() if check_state else None
    data=raw.encode() if raw is not None else (json.dumps(body).encode() if body is not None else None)
    request=urllib.request.Request(base+path,data=data,method=method,headers={'Content-Type':'application/json'})
    try:response=urllib.request.urlopen(request,timeout=12)
    except urllib.error.HTTPError as error:response=error
    with response:
        status=response.status;headers=dict(response.headers);value=json.load(response)
    after=snapshot() if check_state else None
    trace.append({'method':method,'path':path,'request':raw if raw is not None else body,'status':status,
                  'response':value,'location':headers.get('Location'),'before':before,'after':after})
    assert status==expected,(method,path,expected,status,value)
    if status>=400:
        assert set(value)=={'code','message'}
        if check_state:assert before==after,'Rejected request changed saved state'
    elif method=='GET' and check_state:assert before==after
    return value,headers

try:
    start()
    students,_=call('GET','/api/v1/students',200)
    assert len(students)==4
    ids={s['studentNumber']:s['id'] for s in students};ada=ids['ST-100']
    found,_=call('GET','/api/v1/students?search=%20aDa%20',200);assert [x['id'] for x in found]==[ada]
    empty,_=call('GET','/api/v1/students?search=NO-SUCH-STUDENT',200);assert empty==[]
    call('GET','/api/v1/students?search=x&search=y',400)
    call('GET','/api/v1/students?unknown=true',400)
    call('GET',f'/api/v1/students/{ada}',200)
    call('GET','/api/v1/students/2147483647',404)
    call('GET','/api/v1/students/not-an-id',400)
    created,headers=call('POST','/api/v1/students',201,{'studentNumber':'ST-500','fullName':'Eli Tan','contact':None})
    new_id=created['id'];assert created['isEligible'] is True
    assert headers['Location']==f'/api/v1/students/{new_id}'
    call('GET',headers['Location'],200)
    call('PUT',headers['Location'],200,{'studentNumber':'ST-500-A','fullName':'Eli M. Tan','contact':'eli@example.edu'})
    updated,_=call('PUT',headers['Location'],200,{'studentNumber':'ST-500-A','fullName':'Eli M. Tan','contact':None})
    assert updated['id']==new_id and updated['contact'] is None
    for body in [{'studentNumber':'ST-X','fullName':' '},{'studentNumber':' ','fullName':'Test'},
                 {'fullName':'Missing number'},{'studentNumber':'ST-X','fullName':99},
                 {'studentNumber':'ST-X','fullName':'Protected','isEligible':False}]:
        call('POST','/api/v1/students',400,body)
    call('POST','/api/v1/students',400,raw='{broken')
    call('POST','/api/v1/students',409,{'studentNumber':' st-100 ','fullName':'Duplicate'})
    call('PUT',headers['Location'],409,{'studentNumber':' st-100 ','fullName':'Duplicate','contact':None})
    call('PUT',headers['Location'],400,{'studentNumber':'ST-500-A','fullName':'Missing complete contact'})
    call('PUT','/api/v1/students/2147483647',404,{'studentNumber':'ST-X','fullName':'Missing','contact':None})
    loans_before=snapshot()['borrowings']
    changed,_=call('PUT',f'/api/v1/students/{ada}',200,{'studentNumber':'ST-100','fullName':'Ada M. Cruz','contact':None})
    assert changed['id']==ada and snapshot()['borrowings']==loans_before
    for eligible in [False,False,True,True]:
        before=snapshot()
        changed,_=call('PATCH',f'/api/v1/students/{ada}/eligibility',200,{'isEligible':eligible})
        assert changed['isEligible']==eligible and snapshot()['borrowings']==loans_before
        if before['students'][0]['IsEligibleToBorrow']==eligible:assert before==snapshot()
    for body in [{},{'isEligible':None},{'isEligible':'false'},{'isEligible':True,'id':ada}]:
        call('PATCH',f'/api/v1/students/{ada}/eligibility',400,body)
    call('PATCH','/api/v1/students/2147483647/eligibility',404,{'isEligible':False})
    saved=snapshot();stop();start()
    reopened,_=call('GET',headers['Location'],200)
    assert reopened==updated and snapshot()==saved
    # Block only this newly created verification database. Admin remains connected to postgres.
    sql(f'ALTER DATABASE "{args.database}" WITH ALLOW_CONNECTIONS false;', 'postgres')
    sql(f"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='{args.database}';",'postgres')
    try:call('GET','/api/v1/students',500,check_state=False)
    finally:sql(f'ALTER DATABASE "{args.database}" WITH ALLOW_CONNECTIONS true;','postgres')
    call('GET','/api/v1/students',200)
    assert snapshot()==saved
    status,doc_headers,document=None,None,None
    with urllib.request.urlopen(base+'/openapi/v1.json') as response:document=json.load(response)
    operations={value.get('operationId') for path in document['paths'].values() for value in path.values() if isinstance(value,dict)}
    assert {'API-02','API-03','API-04','API-05','API-06'}<=operations
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps({'database':args.database,'campus_date':str(today),'date_policy':'reference offsets from actual Asia/Manila date',
       'dataset':'S1-S4/E1-E7 with supplied history relationships; reset once for student subset',
       'scope':'T02 partial; borrowing/return paths pending','checks':trace,'restart_preserved':True,'openapi_student_operations':sorted(operations)},indent=2)+'\n')
    print(f'PASS: {len(trace)} HTTP requests; independent before/after reads; identity/history/no-op/restart/server failure recovery verified.')
    print('Evidence: '+str(args.output))
finally:
    stop();log.close()
