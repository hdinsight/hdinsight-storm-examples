package microsoft.scp.example.HybridTopology;

import backtype.storm.coordination.BatchOutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseTransactionalBolt;
import backtype.storm.transactional.ICommitter;
import backtype.storm.transactional.TransactionAttempt;
import backtype.storm.tuple.Tuple;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.io.FileWriter;
import java.net.URISyntaxException;
import java.util.Map;

/**
 * Created by tqin on 10/29/2014.
 */
public class TxDisplayer extends BaseTransactionalBolt implements ICommitter {
    public static final Logger LOG = LoggerFactory.getLogger(Displayer.class);
    private static final String LINE_SEPARATOR = System.getProperty("line.separator");  
    private int _taskIndex = -1;
    private String _stormIdSequence;

    // input parameters are only used to test serialization/deserialization of constructor parameters between C# and Java
    public TxDisplayer(int param1, String param2, String param3) {
        LOG.info("TxDisplayer's constructor is called");
        LOG.info("param1: " + param1);

        if (param2 != null) {
            LOG.info("param2: " + param2);
        } else {
            LOG.info("param2: NULL");
        }

        if (param3 != null) {
            LOG.info("param3: " + param3);
        } else {
            LOG.info("param3: NULL");
        }
    }

    @Override
    public void prepare(Map conf, TopologyContext topologyContext, BatchOutputCollector collector, TransactionAttempt attempt) {
        LOG.info("prepare, attempt: " + attempt);
        _taskIndex = topologyContext.getThisTaskIndex();

        String[] parts = topologyContext.getStormId().split("-");
        _stormIdSequence = parts.length > 2 ? parts[2] : Long.toString(System.currentTimeMillis() / 1000);
    }

    @Override
    public void execute(Tuple tuple) {
        LOG.info("execute enter");
        Person person = (Person)tuple.getValue(1);
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

        LOG.info("execute exit");
    }

    @Override
    public void finishBatch() {
        LOG.info("finishBatch");
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
