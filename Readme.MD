经过十多年实战经验积累以及多方共同讨论，[新生命团队](https://github.com/newlifex)制订了一种简单而又具有较好扩展性的`RPC（Remote Procedure Call）`协议。  
全称：`简易远程消息交换协议`，简称： `SRMP（Simple Remote Messaging Protocol）`  

### SRMP目标场景  
SRMP主要定位于以下场景：  
1, 内网高速通信，大吞吐量（>100万tps）、低延迟（<1ms）  
2, 外网远程通信，稳定可靠，海量连接（>10万）  
3, 物联网硬件设备，容易简单实现协议  

### 通信协议  
基于新生命团队标准网络封包协议  
标准网络封包协议：`1 Flag + 1 Sequence + 2 Length + N Payload`  
1个字节标识位，标识请求、响应、错误、加密、压缩等；  
1个字节序列号，用于请求响应包配对；  
2个字节数据长度N，小端字节序，指示后续负载数据长度（不包含头部4个字节），解决粘包问题；  
N个字节负载数据，数据内容完全由业务决定，最大长度65534=64k-2。  

### 指令格式  
默认采用Json序列化，utf-8编码  

请求：S服务名 + N请求参数
响应：S服务名 + 4响应码 + N响应数据

标准指令例子如下：  
请求，00012500 + 05 + Login + 1E + {user:"Stone",pass:"password"}  
响应，80011D00 + 05 + Login + 00000000 + 12 + {data:"大石头"}  
头部4字节，第一个字节最高位区分请求响应，第二个字节序列化，第三第四字节是长度，小字节序  

## 网络库测试用例  
X组件网络库一共经历了4代：  
1，2005年，APM模型，反向代理、点卡服务端  
2，2010年，SAEA模型，P2SP网络，单机7万连接  
3，2014年，物联网云平台，ApiServer单机84.5万连接  
4，2018年，高速RPC，ApiServer单机16万tps  

## 说明文档  
[NewLife.Net——开始网络编程](https://www.cnblogs.com/nnhy/p/newlife_net_echo.html)  
[NewLife.Net——构建可靠的网络服务](https://www.cnblogs.com/nnhy/p/newlife_net_agent.html)  
[NewLife.Net——网络压测单机2266万tps](https://www.cnblogs.com/nnhy/p/newlife_net_benchmark.html)  
[NewLife.Net——管道处理器解决粘包](https://www.cnblogs.com/nnhy/p/newlife_net_handler.html)  
[NetCore版RPC框架NewLife.ApiServer](https://www.cnblogs.com/nnhy/p/newlife_apiserver.html)  

## 压力测试  
NewLife.Net压力测试，峰值4.2Gbps，50万pps，消息大小24字节，消息处理速度2266万tps！  
共集合20台高配ECS参与测试，主服务器带宽6Gbps、100万pps，16核心64G内存。另外19台共模拟400个用户连接，13*16+6*32=400，每用户发送2000万个消息，服务端收到后原样返回。  
![Agent2](Doc/Benchmark/Agent2.png)
![全貌](Doc/RpcTest/全貌.png)

## 新生命开源项目矩阵
各项目默认支持net4.5/net4.0/netstandard2.0  

|                               项目                               | 年份  |  状态  | .NET Core | 说明                                                |
| :--------------------------------------------------------------: | :---: | :----: | :-------: | --------------------------------------------------- |
|                             基础组件                             |       |        |           | 支撑其它中间件以及产品项目                          |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 维护中 |     √     | 算法、日志、网络、RPC、序列化、缓存、多线程         |
|              [XCode](https://github.com/NewLifeX/X)              | 2005  | 维护中 |     √     | 数据中间件，MySQL、SQLite、SqlServer、Oracle        |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 维护中 |     √     | 网络库，千万级吞吐率，学习gRPC、Thrift              |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 维护中 |     √     | Web魔方，企业级快速开发框架，集成OAuth              |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 维护中 |     √     | 服务管理框架，Windows服务、Linux的Systemd           |
|                              中间件                              |       |        |           | 对接各知名中间件平台                                |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | 维护中 |     √     | Redis客户端，微秒级延迟，百亿级项目验证             |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | 维护中 |     √     | 支持Apache RocketMQ和阿里云消息队列，十亿级项目验证 |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 维护中 |     √     | 物联网消息协议，客户端支持阿里云物联网              |
|     [NewLife.LoRa](https://github.com/NewLifeX/NewLife.LoRa)     | 2016  | 维护中 |     √     | 超低功耗的物联网远程通信协议LoRaWAN                 |
|   [NewLife.Thrift](https://github.com/NewLifeX/NewLife.Thrift)   | 2019  | 维护中 |     √     | Thrift协议实现                                      |
|     [NewLife.Hive](https://github.com/NewLifeX/NewLife.Hive)     | 2019  | 维护中 |     √     | 纯托管读写Hive，Hadoop数据仓库，基于Thrift协议      |
|             [NoDb](https://github.com/NewLifeX/NoDb)             | 2017  | 开发中 |     √     | NoSQL数据库，百万级kv读写性能，持久化               |
|      [NewLife.Ftp](https://github.com/NewLifeX/NewLife.Ftp)      | 2008  | 维护中 |     √     | Ftp客户端实现                                       |
|                             产品平台                             |       |        |           | 产品平台级，编译部署即用，个性化自定义              |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 维护中 |     √     | 蚂蚁调度系统，大数据实时计算平台                    |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 维护中 |     √     | 星尘，微服务平台，分布式平台                        |
|            [XLink](https://github.com/NewLifeX/XLink)            | 2016  | 维护中 |     √     | 物联网云平台                                        |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 维护中 |     √     | 产品级反向代理                                      |
|          [XScript](https://github.com/NewLifeX/XScript)          | 2010  | 维护中 |     ×     | C#脚本引擎                                          |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 维护中 |   C++11   | 嵌入式操作系统，完全独立自主，ARM Cortex-M芯片架构  |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | 维护中 |     ×     | Git管理系统                                         |
|                               其它                               |       |        |           |                                                     |
|           [XCoder](https://github.com/NewLifeX/XCoder)           | 2006  | 维护中 |     √     | 码神工具，开发者必备                                |
|        [XTemplate](https://github.com/NewLifeX/XTemplate)        | 2008  | 维护中 |     √     | 模版引擎，T4(Text Template)语法                     |
|       [X组件 .NET2.0](https://github.com/NewLifeX/X_NET20)       | 2002  | 存档中 |  .NET2.0  | 日志、网络、RPC、序列化、缓存、Windows服务、多线程  |

#### 新生命开发团队
`新生命团队始于2002年，部分开源项目具有15年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：http://www.NewLifeX.com  
国外：https://github.com/NewLifeX  
博客：https://nnhy.cnblogs.com  
QQ群：1600800/1600838  
