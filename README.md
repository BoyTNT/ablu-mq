ablu-mq
=======

消息中间件，支持点对点通知、点对点请求/响应、级联消息路由。 （未完成，待补充）


[基本模型]


    EP1 ----> BROKER <---- EP2
                ^
                |
                ------- BROKER <---- EP3

EP1、EP2、EP3之间只要知道对方的标识就可以互相通讯，可以是通知，也可以是请求/响应。
EP与BROKER之间，以及BROKER与BROKER之间是C/S模型，只要求“Client连Server”，不要求反向连接，以适应复杂网络环境。