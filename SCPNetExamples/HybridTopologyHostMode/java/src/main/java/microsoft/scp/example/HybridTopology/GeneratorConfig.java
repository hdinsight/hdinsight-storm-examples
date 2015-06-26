package microsoft.scp.example.HybridTopology;

public class GeneratorConfig {
    private int _m1;
    private String _m2;

    public GeneratorConfig(int m1, String m2) {
        this._m1 = m1;
        this._m2 = m2;
    }

    @Override
    public String toString() {
        return "(_m1: " + this._m1 + ", m2: " + this._m2 + ")";
    }
}