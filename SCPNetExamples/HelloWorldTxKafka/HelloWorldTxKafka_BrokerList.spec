{:name "HelloWorldTxKafka_BrokerList"
 :topology
   (tx-topology
     "HelloWorldTxKafka_BrokerList"
     {
       "generator" 
       (spout-spec 
         (scp-tx-kafka-spout
           {
             "kafka.conf"
               {
                 "topic" "mvlogs"
                 "metadata.broker.list" "127.0.0.1:9092"
                 "kafka.tx.msgcount" 1234    
                 "fetch.size" 3145728
               }
               "output.schema" {"default" ["FDdata"]}
           })
         :p 1)
      }
      
      {
        "partial-count"  
        (bolt-spec
          {
            "generator" :shuffle
          }
          (scp-tx-batch-bolt
            {
              "plugin.name" "HelloWorldKafka.exe"
              "plugin.args" ["partial-count"]
              "output.schema" {"default" ["Count"]}
            })
          :p 1)
          
        "count-sum"  
        (bolt-spec
          {
            "partial-count" :global
          }
          (scp-tx-commit-bolt
            {
              "plugin.name" "HelloWorldKafka.exe"
              "plugin.args" ["count-sum"]
              "output.schema" {"default" ["Sum"]}
            })
          :p 1)
      })
 :config {"topology.kryo.register" ["[B"]}
}
