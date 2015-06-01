{
  :name "HelloWorld"
  :topology
    (nontx-topology
      "HelloWorld"

      {
        "generator" 

        (spout-spec 
          (scp-spout
            {
              "plugin.name" "HelloWorld.exe"
              "plugin.args" ["generator"]
              "output.schema" {"default" ["sentence"]}
              "nontransactional.ack.enabled" true
            })
           
          :p 1)
      }

         
      {
        "splitter"  

        (bolt-spec
          {
            "generator" :shuffle
          }

          (scp-bolt
            {
              "plugin.name" "HelloWorld.exe"
              "plugin.args" ["splitter"]
              "output.schema" {"default" ["word", "firstLetterOfWord"]}
              "nontransactional.ack.enabled" true
            })

          :p 1)

        "counter"  

        (bolt-spec
          {
            "splitter" :global
          }

          (scp-bolt
            {
              "plugin.name" "HelloWorld.exe"
              "plugin.args" ["counter"]
              "output.schema" {"default" ["word" "count"]}
              "nontransactional.ack.enabled" true
            })

          :p 1)
      })

  :config
    {
      "topology.kryo.register" ["[B"]
      "topology.message.timeout.secs" 3
    }
}
