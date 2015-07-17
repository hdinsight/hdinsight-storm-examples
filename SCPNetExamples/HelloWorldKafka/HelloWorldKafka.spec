{
  :name "HelloWorldKafka"
  :topology
    (nontx-topology
      "HelloWorldKafka"

      {
        "generator" 

        (spout-spec 
          (scp-kafka-spout
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

          (scp-bolt
            {
              "plugin.name" "HelloWorldKafka.exe"
              "plugin.args" ["partial-count"]
              "output.schema" {"default" ["bytesNum"]}
            })

          :p 1
          :conf {"topology.tick.tuple.freq.secs" 1})

        "counter-sum"  

        (bolt-spec
          {
            "partial-count" :global
          }

          (scp-bolt
            {
              "plugin.name" "HelloWorldKafka.exe"
              "plugin.args" ["count-sum"]
              "output.schema" {}
            })

          :p 1)
      })

  :config
    {
      "topology.kryo.register" ["[B"]
	  "topology.workers" 3
    }
}
