package microsoft.scp.example.HybridTopology;

import backtype.storm.task.OutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseRichBolt;
import backtype.storm.tuple.Tuple;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.io.FileWriter;
import java.net.URISyntaxException;
import java.util.Map;

/**
 * Created by tqin on 9/11/2014.
 */
public class Displayer extends BaseRichBolt {
    public static final Logger LOG = LoggerFactory.getLogger(Displayer.class);
    private static final String LINE_SEPARATOR = System.getProperty("line.separator");  
    private int _taskIndex = -1;
    private String _stormIdSequence;


    private OutputCollector _collector;

    @Override
    public void prepare(Map map, TopologyContext topologyContext, OutputCollector outputCollector) {
        _collector = outputCollector;
        _taskIndex = topologyContext.getThisTaskIndex();

        String[] parts = topologyContext.getStormId().split("-");
        _stormIdSequence = parts.length > 2 ? parts[2] : Long.toString(System.currentTimeMillis() / 1000);
    }

    @Override
    public void execute(Tuple tuple) {
        Person person = (Person)tuple.getValue(0);
        LOG.info("person: " + person.toString());

        if (_taskIndex == 0)
        {
            try
            {
                String jarPath = GetJarPath(this.getClass());
                String fileName = jarPath + "/../../../../HybridTopologyOutput" + _stormIdSequence +".txt";
                boolean append = true;
                FileWriter fw = new FileWriter(fileName, append);
                fw.write(String.format("person: " + person.toString() + LINE_SEPARATOR));
                fw.close();
            }
            catch (Exception e)
            { }
        }
    }

    @Override
    public void declareOutputFields(OutputFieldsDeclarer outputFieldsDeclarer) {
    }

    private static String GetJarPath(Class clazz)
    {
        String jarPath=".";
        try {
            File jarFile = new File(clazz.getProtectionDomain().getCodeSource().getLocation().toURI().getPath());
            jarPath = jarFile.getParentFile().getPath();
        } catch (URISyntaxException e){ }
        return jarPath;
    }
}
