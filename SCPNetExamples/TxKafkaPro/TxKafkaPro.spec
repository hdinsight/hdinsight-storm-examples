{:name "TxKafkaPro"
 :topology
   (tx-topology
     "TxKafkaPro"
     {
       "generator" 
       (spout-spec 
         (scp-tx-kafka-spout-pro
           {
             "plugin.name" "TxKafkaPro.exe"
             "plugin.args" ["kafkaspout"]
              
             "kafka.conf"
               {
                 "metadata.kafka.zookeeper" "127.0.0.1:2181"
                 "topic" "mvlogs"
                 "kafka.tx.msgcount" 1234    
                 "fetch.size" 3145728
                 "kafka.metadata.refresh.seconds"  600
               }
               "output.schema" {"mydefault" ["StateID" "FDdata"]}
           })
         :p 1)
      }
      
      {
        "partial-count"  
        (bolt-spec
          {
            ["generator" "mydefault"]:shuffle
          }
          (scp-tx-batch-bolt
            {
              "plugin.name" "TxKafkaPro.exe"
              "plugin.args" ["partial-count"]
              "output.schema" {"mydefault" ["Count"]}
            })
          :p 1)
          
        "count-sum"  
        (bolt-spec
          {
            ["partial-count" "mydefault"] :global
          }
          (scp-tx-commit-bolt
            {
              "plugin.name" "TxKafkaPro.exe"
              "plugin.args" ["count-sum"]
              "output.schema" {"mydefault" ["Sum"]}
            })
          :p 1)
      })
 :config {"topology.kryo.register" ["[B"] "topology.workers" 4}
}
