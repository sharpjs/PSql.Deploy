// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class SeedExceptionTests : ExceptionTests<SeedException>
{
    // Tests in base class

    public override void Construct_MessageAndInnerException()
    {
        var inner     = new DataException("Database is on fire.");
        var exception = new SeedException("Yikes!", inner);

        exception.Message.ShouldBe("Yikes! Database is on fire.");
        exception.InnerException.ShouldBeSameAs(inner);
    }

    public void Construct_MessageAndInnerExceptions()
    {
        var inner0    = new DataException("Database is on fire.");
        var inner1    = new InvalidOperationException("Cannot seed when aflame.");
        var inner     = new AggregateException(inner0, inner1);
        var exception = new SeedException("Yikes!", inner);

        exception.Message.ShouldBe("Yikes! Database is on fire. Cannot seed when aflame.");
        exception.InnerException.ShouldBeSameAs(inner);
    }

#if !NET8_0_OR_GREATER
#pragma warning disable SYSLIB0011 // Type or member is obsolete
    public override void SerializeThenDeserialize()
    {
        var inner     = new DataException("Database is on fire.");
        var exception = new SeedException("Yikes!", inner);

        using (var memory = new MemoryStream())
        {
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(memory, exception);
            memory.Position = 0;
            exception = (SeedException) formatter.Deserialize(memory);
        }

        exception.Message.ShouldBe("Yikes! Database is on fire.");
        exception.InnerException.ShouldBeOfType<DataException>();
    }
#pragma warning restore SYSLIB0011 // Type or member is obsolete
#endif
}
