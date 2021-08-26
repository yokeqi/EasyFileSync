# EasyFileSync

#### 介绍
做一款简单、高效、好用的文件同步工具

#### 开发环境
* .NET Framework 4.6
* VisualStudio 2019
* FluentFTP 34.0.2

#### 设计文档
* [简单的类图](https://gitee.com/yokeqi/easy-sync/tree/master/doc/class.jpg)

#### 已实现功能
* 本地文件夹同步（已测试：本地跨盘符拷贝`50个文件`，`25个文件夹`，`6.5M`，`镜像复制`，耗时`367.8543`毫秒，速度贼快）
* 本地 to Ftp（已测试，本地IIS搭建FTP，耗时`1433.5284`毫秒）
* Ftp to 本地（已测试，本地IIS搭建FTP，耗时`1100.5071`毫秒）
* Ftp to Ftp（功能已实现，未测试）

#### 待解决问题
* Ftp与本地文件的一致性校验问题
    * 上传/下载文件保留原文件属性(创建时间、修改时间等)
    * FTP服务器支持获取文件哈希值

#### 参考资料
* [FluentFTP - Github](https://github.com/robinrodricks/FluentFTP)