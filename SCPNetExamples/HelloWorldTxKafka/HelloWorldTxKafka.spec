{:name "HelloWorldTxKafka"
 :topology
   (tx-topology
     "HelloWorldTxKafka"
     {
       "generator" 
       (spout-spec 
         (scp-tx-kafka-spout
           {
             "kafka.conf"
               {
                 "metadata.kafka.zookeeper" "127.0.0.1:2181"
                 "topic" "mvlogs"
                 "kafka.tx.msgcount" 1234    
                 "fetch.size" 3145728
                 "kafka.metadata.refresh.seconds"  600
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
	  "topology.workers" 3
}
