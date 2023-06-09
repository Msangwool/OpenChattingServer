# OpenChattingServer
C# 언어를 이용하여 .net 프레임워크에서 제공하는 Socket 클래스를 통해 채팅 프로토콜을 만들었습니다. console로 client의 요청을 처리할 수 있는 Server와 GUI로 WindowForm을 사용하는 Client 코드로 구성되어 있습니다. 

프로토콜이란, 어떤 데이터 형식 지정 및 처리를 위한 표준화된 규칙 세트 즉, 통신 규약입니다. 따라서 Server와 Client는 메세지를 요청하고 응답받는 과정에서 통신 규약을 지켜야만 합니다.
char[] 를 이용하여 헤더를 구성하지는 않았습니다. 이후에는 char[]를 통해 실제 헤더를 만들어 통신 규약을 강화할 예정입니다.

## 채팅 종류

총 세가지의 채팅을 제공합니다.

1. GroupChatting
    - 서로의 ID를 확인할 수 있습니다.
    - 모든 메세지를 공유합니다.
    - :를 사용하여 귓속말을 할 수 있습니다(더블 클릭으로도 가능합니다).
2. OpenChatting
    - 서로가 누군지는 모르지만 랜덤한 아이디로 구분은 할 수 있습니다.
    - 모든 메세지를 공유합니다.
3. RandomChatting
    - Wait or Start 총 두 가지 상태에 들어갈 수 있습니다.
    - 처음 채팅방에 들어갔을 때는 아무런 동작을 하지 않습니다.
    - Wait 버튼을 누르게 되면 Wait 배열에 들어갑니다. 누군가가 Start를 누르길 기다리는 상태입니다.
    - Start 버튼을 누르게 되면 대화를 시작합니다. Wait 상태인 누군가를 랜덤으로 지정해서 대화를 시작합니다.
    
    **Exception Handling**
    
    - 채팅 도중 상대방이 나가거나 Wait or Start를 누르면 상대방이 채팅을 종료했다는 알림을 받습니다. 다시 Wait or Start를 누를 수 있습니다.
    - 채팅 도중 Wait을 누르면 상대방과의 대화는 종료되고 Wait 상태가 됩니다.
    - 채팅 도중 Start를 누르면 상대방과의 대화는 종료되고 Wait 상태인 누군가와 랜덤으로 대화를 시작합니다.
    - Wait 상태인 사람이 하나도 없을 때, Start를 누르게 되면 대화할 수 있는 상대가 없다는 메세지를 받습니다. 다시 Wait or Start를 누를 수 있습니다.

## Group Chatting


https://github.com/Msangwool/OpenChattingClient/assets/97933061/3e92f66e-82de-48e2-b4bf-66bf1411bca5



## Open Chatting


https://github.com/Msangwool/OpenChattingClient/assets/97933061/d042f04e-88a7-4ac9-a9ff-024543bd0775



## Random Chatting


https://github.com/Msangwool/OpenChattingClient/assets/97933061/d8c13639-26a0-4f3f-a11c-f061285ae8f3

코드에 대한 자세한 내용은 Blog에 작성해 놓았습니다. <br>
Notion : https://gratis-somersault-5bf.notion.site/OpenChatting-Protocol-8acd30fb6d5a4fba9e1cc89d8322ef4f?pvs=4
<br>

OpenChattingServer : https://github.com/Msangwool/OpenChattingClient
