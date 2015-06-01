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
            })

          :p 1)

        "counter"  

        (bolt-spec
          {
            "splitter" (scp-field-group :non-tx [1])
          }

          (scp-bolt
            {
              "plugin.name" "HelloWorld.exe"
              "plugin.args" ["counter"]
              "output.schema" {"default" ["word" "count"]}
            })

          :p 1)
      })

  :config
    {
      "topology.kryo.register" ["[B"]
    }
}
