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
------------

## 받은 패킷 순차 처리 과정

1. 받기 완료 요청 오면 ReceiveCompleted에서 Context 생성
2. Receiver Decrypt에서 복호화, 압축 해제 후 Packet 생성
3. Receiver Process에서 Queue에 생성 된 Packet이 있으면 NetworkService의 ContextDic에 AccountID 별로 Context를 넣는다.
4. ContextDic에 AccountID로 처음 들어가는 Context는 ContextQueue에 넣는다.
5. Parser Process에서 NetworkService ContextQueue에 데이터가 있으면 꺼내와서 ProcessContext를 실행한다.
6. Parser ProcessContext에서 패킷 처리를 하고 패킷 처리가 완료 되면 NetworkService ReleaseContext로 꺼내올 패킷이 있는지 체크 후 있으면 꺼내와서 다시 처리를 계속 한다.

------------
