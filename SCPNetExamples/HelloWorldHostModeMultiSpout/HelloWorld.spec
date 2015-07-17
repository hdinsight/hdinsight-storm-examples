{:name "HelloWorld"
 :topology
   (topology

     {
       "SentenceGenerator"
       (spout-spec 
         (scp-spout
           {
             "plugin.name" "SCPHost.exe"
             "plugin.args" ["HelloWorld.dll" "Scp.App.HelloWorld.SentenceGenerator" "Get" "SentenceGenerator.config"]
             "output.schema" {"SentenceStream" ["sentence"]}
           })
         :p 1)

       "PersonGenerator"
       (spout-spec 
         (scp-spout
           {
             "plugin.name" "SCPHost.exe"
             "plugin.args" ["HelloWorld.dll" "Scp.App.HelloWorld.PersonGenerator" "Get"]
             "output.schema" {"PersonStream" ["person"]}
           })
         :p 1)
     }

     {
       "displayer"
       (bolt-spec 
         {
           ["SentenceGenerator" "SentenceStream"] :shuffle
           ["PersonGenerator" "PersonStream"] :shuffle
         }
         (scp-bolt
           {
             "plugin.name" "SCPHost.exe"
             "plugin.args" ["HelloWorld.dll" "Scp.App.HelloWorld.Displayer" "Get"]
             "output.schema" {}
           })
         :p 1)
     }

   )

 :config {"topology.kryo.register" ["[B"]}

}
