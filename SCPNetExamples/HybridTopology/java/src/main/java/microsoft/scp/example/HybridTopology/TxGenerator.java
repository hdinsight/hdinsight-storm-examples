package microsoft.scp.example.HybridTopology;

import backtype.storm.coordination.BatchOutputCollector;
import backtype.storm.spout.SpoutOutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseRichSpout;
import backtype.storm.topology.base.BaseTransactionalSpout;
import backtype.storm.transactional.ITransactionalSpout;
import backtype.storm.transactional.TransactionAttempt;
import backtype.storm.tuple.Fields;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.math.BigInteger;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Random;

/**
 * Created by tqin on 10/29/2014.
 */
public class TxGenerator extends BaseTransactionalSpout {
    public static final Logger LOG = LoggerFactory.getLogger(Generator.class);

    class TxGeneratorCoordinator implements Coordinator <Integer> {

        public TxGeneratorCoordinator(Map conf, TopologyContext context) {

        }

        @Override
        public Integer initializeTransaction(BigInteger txid, Integer prevMetadata) {
            return 0;
        }

        @Override
        public boolean isReady() {
            return true;
        }

        @Override
        public void close() {
        }
    }

    class TxGeneratorEmitter implements Emitter<Integer> {
        Random _rand;

        Person[] persons = new Person[] {
                new Person("Tom", 20),
                new Person("Marry", 18),
                new Person("David", 25)
        };

        public TxGeneratorEmitter(Map conf, TopologyContext context) {
            _rand = new Random();
        }

        @Override
        public void emitBatch(TransactionAttempt tx, Integer coordinatorMeta, BatchOutputCollector collector) {
            for (int i = 0; i < 3; i++) {
                Person person = persons[_rand.nextInt(persons.length)];
                LOG.info("person: " + person.toString());
                List<Object> toEmit = new ArrayList<Object>(2);
                toEmit.add(tx);
                toEmit.add(person);
                collector.emit("default", toEmit);
            }
        }

        @Override
        public void cleanupBefore(BigInteger txid) {
        }

        @Override
        public void close() {
        }

    }

    @Override
    public ITransactionalSpout.Coordinator<Integer> getCoordinator(Map conf, TopologyContext context) {
        LOG.info("ScpNetTxSpout getCoordinator");
        return new TxGeneratorCoordinator(conf, context);
    }

    @Override
    public ITransactionalSpout.Emitter<Integer> getEmitter(Map conf, TopologyContext context) {
        LOG.info("ScpNetTxSpout getEmitter");
        return new TxGeneratorEmitter(conf, context);
    }


    public void declareOutputFields(OutputFieldsDeclarer outputFieldsDeclarer) {
        outputFieldsDeclarer.declareStream("default", new Fields("id", "person"));
    }
}
