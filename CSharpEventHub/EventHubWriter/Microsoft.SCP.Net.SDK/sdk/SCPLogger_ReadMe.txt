SCPLogger is designed to be a flexible logger plugin framework for SCP.Net
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
To customize your own Logger Instance, you can follow these:
1. Write your own logger factory which implements the ILoggerFactory Interface, you can do some initialize work here if needed(such as setting the required config/env)
2. Write your own logger which inherits from the LoggerBase, then you need to implement the abstract Log method. Also you can decide if you need to override the IsLogEnabled method.

Here's an example with log4net:
1. Create the Log4netLoggerFactory as follows:
     public class Log4netLoggerFactory : ILoggerFactory
     {
		//did some initialize work

        public ILogger GetLogger(string name)
        {
            return new Log4netLogger(LogManager.GetLogger(name));
        }
     }
Besides return the ILogger instance, for log4net needs its own config to config the log4net logger, we can provide a default one during the inialize step in case user didn't specify theirs.
2. create the Log4netLogger:
     public class Log4netLogger:LoggerBase
    {
	     private readonly ILog logger; //real log4net logger instance

		 public Log4netLogger(ILog logger)
		 {
			...
		 }

	     public override void Log(LogLevel level, string title, string message, Exception ex)
        {
			...
		}
		public override bool IsLogEnabled(LogLevel level)
        {
		...
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
we've defined our logger, then how to config to use it? now we define two ways to do this:
1. Use the Assemly attribute [assembly:SCPLogger("<full name of your own loggerFactory>")]
   If your don't want to customize your own logger config(like log4net), you can use this way to simplify your develop. 
   the disadvantage of this is that if you want to change to a different logger, you need to change the attribute and recompile your app.

2. User app config to config the logger:
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<	
First let's see an simple example about how to use the logger we defined above:
<?xml version="1.0"?>
<configuration>
  <configSections>
    <section name="SCPLogger" type="Microsoft.SCP.SCPLogger.Config.SCPLogger, Microsoft.SCPLogger"/>
  </configSections>
  <SCPLogger>
    <LoggerFactories>
      <LoggerFactory type="SCPLogger.Log4net.Log4netLoggerFactory, SCPLogger.Log4net" />
    </LoggerFactories>
  </SCPLogger>
</configuration>
This is the simplest case, you just need to define the logger factory you want to use here. the section SCPLogger is the one we need to look up the logger
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
Next let's see a more complex example:
<?xml version="1.0"?>
<configuration>
  <configSections>
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net"/>
    <section name="SCPLogger" type="Microsoft.SCP.SCPLogger.Config.SCPLogger, Microsoft.SCPLogger"/>
  </configSections>
  <SCPLogger>
    <LoggerFactories>
      <LoggerFactory name="log4net" type="SCPLogger.Log4net.Log4netLoggerFactory, SCPLogger.Log4net" />
    </LoggerFactories>
    <Loggers>
      <Logger name="T_1" factory="log4net" />
      <Logger name="T_1_error" factory="log4net" level="Error" />
    </Loggers>
  </SCPLogger>
  <log4net>
    <appender name="ErrorAppender" type="log4net.Appender.RollingFileAppender">
      ...
    </appender>
    <appender name="RollingFileAppender" type="log4net.Appender.RollingFileAppender">
      ...
    </appender>
    <logger name="logerror">
      <level value="ERROR" />
      <appender-ref ref="ErrorAppender" />
    </logger>
    <root>
      <level value="INFO"/>
      <appender-ref ref="RollingFileAppender"/>
    </root>
  </log4net>
</configuration>
in this example, i defined a logger factory: Log4netLoggerFacotry with name "log4net". then i defined 2 loggers based on the logger factory. 
 <Logger name="T_1" factory="log4net" />: this means that i want to define a logger which uses the Log4netLoggerFacotry and the default log Level is All(not specified).
 <Logger name="T_1_error" factory="log4net" level="Error" />: this means that i want to define a logger which still uses the Log4netLoggerFacotry but the log Level is Error.

the log levels we now defined include:
        All,
        Debug,
        Info,
        Warn,
        Error,
        Fatal   

if you didn't specify the level attribute for Logger element, the default level is ALL. 
If your inner used logger has specified log level, there will be 3 cases:
a. you didn't specify logger level in SCPLogger config, then it will default to use the inner log's defaul level.
b. inner logger's default level is less that the level you specified in config. in this case, the level you defined in config will take advantage.
c. inner logger's default level is larger than the level you specified in config. in this case, the inner log's level will take advantage. you should be careful of such case, usually this means
that you have a config error.
Take log4net for example, as we saw from the config, its root logger's level is set to INFO, thus for logger "T_1" the default log level will be Info;
but for logger "T_1_error" the default log level is Error.
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
How to use the logger in our programe?
we can use LoggerManager.GetLogger("T_1") to get the logger we defined. 
Also For some special kind logger, such as the log4net logger,since it can define its own logger in config(like the log4net section), we can also use these two ways to get the logger:
1. LoggerManager.GetLogger("logerror"): this is to get the error logger defined in log4net section as the inner logger.
2. LoggerManager.GetLogger("<your class name>"): this is to get the default root logger of log4net, and it will use the <your class name> as the title when doing logging.
For other logger, you can still use the above two ways to retrieve the logger, but ususally they will be resolved to one same logger to do logging.

