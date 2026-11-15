"""Real HTTP student/equipment verification in a new disposable PostgreSQL database.
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
parser.add_argument("--resource", choices=["students", "equipment", "borrowings"], default="students")
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
    if args.resource == "students":
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
        items,_=call('GET','/api/v1/equipment',200)
        item_ids={item['equipmentCode']:item['id'] for item in items}
        e1_loan=next(loan for loan in snapshot()['borrowings'] if loan['StudentId']==ada and loan['EquipmentId']==item_ids['EQ-001'])
        known,_=call('GET',f'/api/v1/borrowings/{e1_loan["Id"]}',200)
        call('PATCH',f'/api/v1/students/{ada}/eligibility',200,{'isEligible':False})
        assert snapshot()['borrowings']==loans_before
        due=str(today+datetime.timedelta(days=2))
        call('POST','/api/v1/borrowings',409,{'studentId':ada,'equipmentId':item_ids['EQ-002'],'expectedReturnDate':due})
        returned,_=call('POST',f'/api/v1/borrowings/{known["id"]}/return',200,{})
        assert returned['id']==known['id'] and returned['status']=='Returned' and returned['returnedDate']==str(today)
        call('PATCH',f'/api/v1/students/{ada}/eligibility',200,{'isEligible':True})
        issued,_=call('POST','/api/v1/borrowings',201,{'studentId':ada,'equipmentId':item_ids['EQ-001'],'expectedReturnDate':due})
        assert issued['id']!=returned['id'] and issued['status']=='Active'
        before=snapshot()
        call('PATCH',f'/api/v1/students/{ada}/eligibility',200,{'isEligible':True})
        assert snapshot()==before
    elif args.resource == "equipment":
        items,_=call('GET','/api/v1/equipment',200)
        assert len(items)==7
        ids={item['equipmentCode']:item['id'] for item in items};camera=ids['EQ-001']
        available,_=call('GET','/api/v1/equipment?available=true',200)
        assert {item['equipmentCode'] for item in available}=={'EQ-002','EQ-003','EQ-006','EQ-007'}
        all_items,_=call('GET','/api/v1/equipment?available=false',200);assert all_items==items
        for path in ['?available=garbage','?available=','?available=true&available=false','?unknown=true']:
            call('GET','/api/v1/equipment'+path,400)
        call('GET',f'/api/v1/equipment/{camera}',200)
        call('GET','/api/v1/equipment/2147483647',404)
        call('GET','/api/v1/equipment/not-an-id',400)
        created,headers=call('POST','/api/v1/equipment',201,{'equipmentCode':'EQ-008','name':'Monitor','description':None})
        new_id=created['id'];assert created['isAvailable'] is True
        assert headers['Location']==f'/api/v1/equipment/{new_id}'
        call('GET',headers['Location'],200)
        call('PUT',headers['Location'],200,{'equipmentCode':'EQ-008-A','name':'Portable Monitor','description':'USB-C display'})
        updated,_=call('PUT',headers['Location'],200,{'equipmentCode':'EQ-008-A','name':'Portable Monitor','description':None})
        assert updated['id']==new_id and updated['description'] is None and updated['isAvailable'] is True
        for body in [{'equipmentCode':'EQ-X','name':' '},{'equipmentCode':' ','name':'Test'},
                     {'name':'Missing code'},{'equipmentCode':'EQ-X','name':99},
                     {'equipmentCode':'EQ-X','name':'Protected','isAvailable':False}]:
            call('POST','/api/v1/equipment',400,body)
        omitted,_=call('POST','/api/v1/equipment',201,{'equipmentCode':'EQ-009','name':'Cable'})
        assert omitted['description'] is None and omitted['isAvailable'] is True
        call('POST','/api/v1/equipment',400,raw='{broken')
        call('POST','/api/v1/equipment',409,{'equipmentCode':' eq-001 ','name':'Duplicate'})
        call('PUT',headers['Location'],409,{'equipmentCode':' eq-001 ','name':'Duplicate','description':None})
        call('PUT',headers['Location'],400,{'equipmentCode':'EQ-008-A','name':'Missing complete description'})
        call('PUT',headers['Location'],400,{'equipmentCode':'EQ-008-A','name':' ','description':None})
        call('PUT','/api/v1/equipment/2147483647',404,{'equipmentCode':'EQ-X','name':'Missing','description':None})
        loans_before=snapshot()['borrowings']
        for key,value in [('isAvailable',True),('id',camera),('status','Returned'),('unexpected','value')]:
            protected={'equipmentCode':'EQ-001','name':'Camera','description':None,key:value}
            call('PUT',f'/api/v1/equipment/{camera}',400,protected)
        changed,_=call('PUT',f'/api/v1/equipment/{camera}',200,
                       {'equipmentCode':'EQ-001-A','name':'Camera Kit','description':None})
        assert changed['id']==camera and changed['isAvailable'] is False
        assert snapshot()['borrowings']==loans_before
        # Empty available results use explicit temporary query-fixture state in this disposable DB only.
        previous=snapshot()
        sql('UPDATE "Equipment" SET "IsAvailable"=false;')
        empty,_=call('GET','/api/v1/equipment?available=true',200);assert empty==[]
        for item in previous['equipment']:
            value='true' if item['IsAvailable'] else 'false'
            sql(f'UPDATE "Equipment" SET "IsAvailable"={value} WHERE "Id"={item["Id"]};')
        assert snapshot()==previous
    else:
        students,_=call('GET','/api/v1/students',200)
        items,_=call('GET','/api/v1/equipment',200)
        student_ids={s['studentNumber']:s['id'] for s in students}
        item_ids={item['equipmentCode']:item['id'] for item in items}
        due=str(today+datetime.timedelta(days=2))
        valid={'studentId':student_ids['ST-400'],'equipmentId':item_ids['EQ-002'],'expectedReturnDate':due}
        created,headers=call('POST','/api/v1/borrowings',201,valid)
        loan_id=created['id']
        assert headers['Location']==f'/api/v1/borrowings/{loan_id}'
        assert created['status']=='Active' and created['borrowedDate']==str(today) and created['returnedDate'] is None
        assert created['studentId']==valid['studentId'] and created['equipmentId']==valid['equipmentId']
        assert next(item for item in snapshot()['equipment'] if item['Id']==valid['equipmentId'])['IsAvailable'] is False
        stored,_=call('GET',headers['Location'],200);assert stored==created
        for changes,expected in [({'studentId':student_ids['ST-200'],'equipmentId':item_ids['EQ-006']},409),
                                 ({'equipmentId':item_ids['EQ-001']},409),({'studentId':2147483647,'equipmentId':item_ids['EQ-006']},404),
                                 ({'equipmentId':2147483647},404),({'expectedReturnDate':str(today)},400),
                                 ({'expectedReturnDate':str(today-datetime.timedelta(days=1))},400),
                                 ({'studentId':0},400),({'equipmentId':-1},400),({'expectedReturnDate':'bad-date'},400),
                                 ({'studentId':'invalid'},400),({'expectedReturnDate':None},400),
                                 ({'id':999},400),({'status':'Returned'},400),({'borrowedDate':str(today)},400),
                                 ({'returnedDate':str(today)},400)]:
            call('POST','/api/v1/borrowings',expected,{**valid,**changes})
        for missing in ['studentId','equipmentId','expectedReturnDate']:
            call('POST','/api/v1/borrowings',400,{key:value for key,value in valid.items() if key!=missing})
        call('POST','/api/v1/borrowings',400,raw='{broken')
        call('GET','/api/v1/borrowings/2147483647',404)
        call('GET','/api/v1/borrowings/not-an-id',400)
        for body in [{'returnedDate':str(today)},{'status':'Returned'},{'unexpected':True}]:
            call('POST',headers['Location']+'/return',400,body)
        call('POST',headers['Location']+'/return',400,raw='null')
        call('POST',headers['Location']+'/return',400)
        call('POST','/api/v1/borrowings/2147483647/return',404,{})
        updated,_=call('POST',headers['Location']+'/return',200,{})
        assert updated['id']==loan_id and updated['status']=='Returned' and updated['returnedDate']==str(today)
        assert updated['borrowedDate']==created['borrowedDate'] and updated['expectedReturnDate']==due
        assert next(item for item in snapshot()['equipment'] if item['Id']==valid['equipmentId'])['IsAvailable'] is True
        stored,_=call('GET',headers['Location'],200);assert stored==updated
        call('POST',headers['Location']+'/return',409,{})
    saved=snapshot();stop();start()
    reopened,_=call('GET',headers['Location'],200)
    assert reopened==updated and snapshot()==saved
    # Block only this newly created verification database. Admin remains connected to postgres.
    sql(f'ALTER DATABASE "{args.database}" WITH ALLOW_CONNECTIONS false;', 'postgres')
    sql(f"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='{args.database}';",'postgres')
    probe_path=headers['Location'] if args.resource=='borrowings' else '/api/v1/'+args.resource
    try:call('GET',probe_path,500,check_state=False)
    finally:sql(f'ALTER DATABASE "{args.database}" WITH ALLOW_CONNECTIONS true;','postgres')
    call('GET',probe_path,200)
    assert snapshot()==saved
    status,doc_headers,document=None,None,None
    with urllib.request.urlopen(base+'/openapi/v1.json') as response:document=json.load(response)
    operations={value.get('operationId') for path in document['paths'].values() for value in path.values() if isinstance(value,dict)}
    required={'students':{'API-02','API-03','API-04','API-05','API-06'}, 'equipment':{'API-01','API-07','API-08','API-09'}, 'borrowings':{'API-10','API-12','API-13'}}[args.resource]
    assert required<=operations
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps({'database':args.database,'campus_date':str(today),'date_policy':'reference offsets from actual Asia/Manila date',
       'dataset':'S1-S4/E1-E7 with supplied history relationships; reset once for this test',
       'scope':{'students':'T02 complete execution','equipment':'T03 equipment execution','borrowings':'T04 single borrowing/return execution'}[args.resource],'checks':trace,'restart_preserved':True,'openapi_student_operations':sorted(operations)},indent=2)+'\n')
    print(f'PASS: {len(trace)} HTTP requests; independent before/after reads; identity/history/protected-state/restart/server failure recovery verified.')
    print('Evidence: '+str(args.output))
finally:
    stop();log.close()
