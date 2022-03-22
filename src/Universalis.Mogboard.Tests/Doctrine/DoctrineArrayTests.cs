using Universalis.Mogboard.Doctrine;
using Xunit;

namespace Universalis.Mogboard.Tests.Doctrine;

public class DoctrineArrayTests
{
    [Fact]
    public void Array_Parsing_Works_Zero()
    {
        const string data = "a:0:{}";
        var arr = DoctrineArray<int>.Parse(data);
        Assert.Empty(arr);
    }

    [Fact]
    public void Array_Serialization_Works_Zero()
    {
        const string data = "a:0:{}";
        var arr = DoctrineArray<int>.Parse(data);
        var s = arr.ToString();
        Assert.Equal(data, s);
    }

    [Fact]
    public void Array_Parsing_Works_One()
    {
        const string data = "a:1:{i:0;i:24;}";
        var arr = DoctrineArray<int>.Parse(data);
        Assert.Single(arr);
        Assert.Equal(24, arr[0]);
    }

    [Fact]
    public void Array_Serialization_Works_One()
    {
        const string data = "a:1:{i:0;i:24;}";
        var arr = DoctrineArray<int>.Parse(data);
        var s = arr.ToString();
        Assert.Equal(data, s);
    }

    [Fact]
    public void Array_Parsing_Works_Two()
    {
        const string data = "a:2:{i:0;i:24;i:1;i:30;}";
        var arr = DoctrineArray<int>.Parse(data);
        Assert.Equal(2, arr.Length);
        Assert.Equal(24, arr[0]);
        Assert.Equal(30, arr[1]);
    }

    [Fact]
    public void Array_Serialization_Works_Two()
    {
        const string data = "a:2:{i:0;i:24;i:1;i:30;}";
        var arr = DoctrineArray<int>.Parse(data);
        var s = arr.ToString();
        Assert.Equal(data, s);
    }

    [Fact]
    public void Array_Parsing_Works_Many()
    {
        const string data = "a:41:{i:0;i:33931;i:1;i:27109;i:2;i:28511;i:3;i:34542;i:4;i:35765;i:5;i:33903;i:6;i:31820;i:7;i:27155;i:8;i:35356;i:9;i:22887;i:10;i:35353;i:11;i:33901;i:12;i:26440;i:13;i:34243;i:14;i:35343;i:15;i:35396;i:16;i:34538;i:17;i:29407;i:18;i:26443;i:19;i:29958;i:20;i:34242;i:21;i:34696;i:22;i:34686;i:23;i:34698;i:24;i:30854;i:25;i:36215;i:26;i:36207;i:27;i:36251;i:28;i:35465;i:29;i:35466;i:30;i:35397;i:31;i:34697;i:32;i:35440;i:33;i:35394;i:34;i:34690;i:35;i:34682;i:36;i:33895;i:37;i:35401;i:38;i:34691;i:39;i:34746;i:40;i:26433;}";
        var arr = DoctrineArray<int>.Parse(data);
        Assert.Equal(41, arr.Length);
        Assert.Equal(33931, arr[0]);
        Assert.Equal(26433, arr[40]);
    }

    [Fact]
    public void Array_Serialization_Works_Many()
    {
        const string data = "a:41:{i:0;i:33931;i:1;i:27109;i:2;i:28511;i:3;i:34542;i:4;i:35765;i:5;i:33903;i:6;i:31820;i:7;i:27155;i:8;i:35356;i:9;i:22887;i:10;i:35353;i:11;i:33901;i:12;i:26440;i:13;i:34243;i:14;i:35343;i:15;i:35396;i:16;i:34538;i:17;i:29407;i:18;i:26443;i:19;i:29958;i:20;i:34242;i:21;i:34696;i:22;i:34686;i:23;i:34698;i:24;i:30854;i:25;i:36215;i:26;i:36207;i:27;i:36251;i:28;i:35465;i:29;i:35466;i:30;i:35397;i:31;i:34697;i:32;i:35440;i:33;i:35394;i:34;i:34690;i:35;i:34682;i:36;i:33895;i:37;i:35401;i:38;i:34691;i:39;i:34746;i:40;i:26433;}";
        var arr = DoctrineArray<int>.Parse(data);
        var s = arr.ToString();
        Assert.Equal(data, s);
    }
}