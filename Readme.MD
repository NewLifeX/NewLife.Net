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

#### 新生命开发团队  
博客：https://nnhy.cnblogs.com  
QQ群：1600800  

## 项目位置  
`注意：X组件具有15年漫长历史，源码库保留有2010年以来所有修改记录，并一直保持更新，请确保获取得到最新版本源代码`  
https://github.com/NewLifeX/NewLife.Net  

## 新生命开源项目矩阵

|                               项目                               | 年份  |  状态  | .NET Core | 说明                                               |
| :--------------------------------------------------------------: | :---: | :----: | :-------: | -------------------------------------------------- |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 维护中 |     √     | 日志、网络、RPC、序列化、缓存、多线程 |
|              [XCode](https://github.com/NewLifeX/X)              | 2005  | 维护中 |     √     | 数据中间件，MySQL、SQLite、SqlServer、Oracle       |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 维护中 |     √     | Web魔方，权限基础框架，集成OAuth                   |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | 维护中 |     √     | Redis客户端，微秒级延迟，百亿级项目验证            |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | 维护中 |     √     | 支持Apache RocketMQ和阿里云消息队列                |
|       [NewLife.MQ](https://github.com/NewLifeX/NewLife.MQ)       | 2016  | 维护中 |     √     | 轻量级消息队列                                     |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 开发中 |     √     | 星尘，微服务平台，学习Dubbo                        |
|           [XAgent](https://github.com/NewLifeX/XAgent)           | 2008  | 重构中 |     √     | 集群节点分布式计算平台，学习Hadoop                 |
|             [NoDb](https://github.com/NewLifeX/NoDb)             | 2017  | 开发中 |     √     | NoSQL数据库，百万级读写性能，学习Redis+MongoDB     |
|    [NewLife.Cache](https://github.com/NewLifeX/NewLife.Cache)    | 2018  | 维护中 |     √     | 自定义缓存服务器                                   |
|            [XLink](https://github.com/NewLifeX/XLink)            | 2016  | 维护中 |     √     | 物联网云平台                                       |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 维护中 |     √     | 网络库，千万级吞吐率，学习gRPC、Thrift             |
|           [XCoder](https://github.com/NewLifeX/XCoder)           | 2006  | 维护中 |     ×     | 码神工具，开发者必备                               |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 维护中 |     √     | 产品级反向代理                                     |
|          [XScript](https://github.com/NewLifeX/XScript)          | 2010  | 维护中 |     ×     | C#脚本引擎                                         |
|        [XTemplate](https://github.com/NewLifeX/XTemplate)        | 2008  | 维护中 |     ×     | 模版引擎，学习TT                                   |
|      [NewLife.DNS](https://github.com/NewLifeX/NewLife.DNS)      | 2011  | 维护中 |     ×     | DNS代理服务器                                      |
|      [NewLife.CMX](https://github.com/NewLifeX/NewLife.CMX)      | 2013  | 维护中 |     ×     | 内容管理系统                                       |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 保密中 |   C++11   | 嵌入式操作系统，面向ARM Cortex-M芯片架构           |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | 维护中 |     ×     | Git管理系统                                        |
|    [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql)    | 2018  | 开发中 |     √     | MySql驱动                                          |
|       [X组件 .NET2.0](https://github.com/NewLifeX/X_NET20)       | 2002  | 存档中 |  .NET2.0  | 日志、网络、RPC、序列化、缓存、Windows服务、多线程 |
|       [X组件 .NET4.0](https://github.com/NewLifeX/X_NET40)       | 2002  | 存档中 |  .NET4.0  | 日志、网络、RPC、序列化、缓存、Windows服务、多线程 |
