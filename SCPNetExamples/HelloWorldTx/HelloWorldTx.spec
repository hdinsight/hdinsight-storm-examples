{:name "HelloWorldTx"
 :topology
   (tx-topology
     "HelloWorldTx"
     {
       "generator" 
       (spout-spec 
         (scp-tx-spout
           {
             "plugin.name" "HelloWorldTx.exe"
             "plugin.args" ["generator"]
             "output.schema" {"default" ["filename"]}
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
              "plugin.name" "HelloWorldTx.exe"
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
              "plugin.name" "HelloWorldTx.exe"
              "plugin.args" ["count-sum"]
              "output.schema" {"default" ["sum"]}
            })
          :p 1)
      })
 :config {"topology.kryo.register" ["[B"]}
}
