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
                 "topic" "mvlogs"
                 "metadata.broker.list" "127.0.0.1:9092"
                 "kafka.tx.msgcount" 1234    
                 "fetch.size" 3145728
               }
             "output.schema" {"default" ["FDdata"]}
             "nontransactional.ack.enabled" true
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
              "nontransactional.ack.enabled" true
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
              "nontransactional.ack.enabled" true
            })

          :p 1)
      })

  :config
    {
      "topology.kryo.register" ["[B"]
      "topology.message.timeout.secs" 30
    }
}
