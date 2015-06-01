package microsoft.scp.example.HybridTopology;

public class Person implements java.io.Serializable {
    public String name;
    public int age;

    public Person(String name, int age)
    {
        this.name = name;
        this.age = age;
    }

    @Override
    public String toString() {
        return "(name: " + name + ", age: " + age + ")";
    }
}
