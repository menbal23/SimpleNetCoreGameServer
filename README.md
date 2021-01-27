# SimpleNetCoreGameServer

> 쉽게 따라 할수 있는 게임 서버 입니다.
> 모든 패킷은 순차 처리 됩니다.
> async awit로 DB 처리를 싱글 스레드 처럼 코드 작성이 가능 합니다.

------------

## 사용 방법

1. 초기화를 진행한다.
```C#
BufferManager.Instance.Initialize();
PlayerManager.Instance.Initialize(1000);
ServerManager.Instance.Initialize();
```

2. ini 파일에 필요한 정보를 입력 후 사용한다.
```C#
m_Config = new IniUtil(Environment.CurrentDirectory + @"\server.ini");
Listener.Instance.Init(int.Parse(m_Config.GetIniValue("Server", "Port")));
```

3. 스레드를 생성 한다.
```C#
Thread thread = new Thread(Process);
thread.Start();
thread.Join();
```

4. Protocol에서 Packet생성 후  ServerManager에서 사용 할 Packet을 등록한다.
```C#
RegisterPacket(new PacketConnectReq(), (short)PROTOCOL.CONNECT_ACK, RecvConnectReq);
```

5. MS-SQL 사용 시 DB 정보를 세팅한다.
```C#
string DBName = m_Config.GetIniValue("DB", "NAME");
string DBIP = m_Config.GetIniValue("DB", "IP");
string DBID = m_Config.GetIniValue("DB", "ID");
string DBPW = m_Config.GetIniValue("DB", "PW");

m_DBInfo = string.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3};", DBIP, DBName, DBID, DBPW);
```

6. DB 사용은 아래와 같다
```C#
SQLDB sql = new SQLDB("DB 정보");

sql.SetProcedure("프로시저 이름");
sql.Add("파라메타 정보");

switch (await sql.Execute())
{
	// 성공
	case 0:
		break;
	// 실패
	default:
		break;
}
```

7. 빌드시 Config 폴더에 있는 ini 파일을 debug 폴더에 넣어야 한다.
```
Server.ini => Server\bin\Debug\netcoreapp3.1
Client.ini => Client\bin\Debug\netcoreapp3.1
Client.ini => Client\bin\Debug\netcoreapp3.1\win-x64
```

------------

## 받은 패킷 순차 처리 과정

1. 받기 완료 요청 오면 ReceiveCompleted에서 Context 생성
2. Receiver Decrypt에서 복호화, 압축 해제 후 Packet 생성
3. Receiver Process에서 Queue에 생성 된 Packet이 있으면 NetworkService의 ContextDic에 AccountID 별로 Context를 넣는다.
4. ContextDic에 AccountID로 처음 들어가는 Context는 ContextQueue에 넣는다.
5. Parser Process에서 NetworkService ContextQueue에 데이터가 있으면 꺼내와서 ProcessContext를 실행한다.
6. Parser ProcessContext에서 패킷 처리를 하고 패킷 처리가 완료 되면 NetworkService ReleaseContext로 꺼내올 패킷이 있는지 체크 후 있으면 꺼내와서 다시 처리를 계속 한다.

------------

## 추후 기능 추가 예정
+ 로그 기능
+ 덤프 기능
+ 데이터 파일 로드
+ 웹 인증(구글, 페이스북...)
