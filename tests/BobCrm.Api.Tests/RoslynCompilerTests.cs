using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BobCrm.Api.Tests;

public class RoslynCompilerTests
{
    private readonly RoslynCompiler _compiler;
    private readonly Mock<ILogger<RoslynCompiler>> _mockLogger;

    public RoslynCompilerTests()
    {
        _mockLogger = new Mock<ILogger<RoslynCompiler>>();
        _compiler = new RoslynCompiler(_mockLogger.Object);
    }

    [Fact]
    public void Compile_ShouldSucceed_WithValidCode()
    {
        // Arrange
        var sourceCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public void TestMethod()
        {
            Console.WriteLine(""Hello World"");
        }
    }
}";

        // Act
        var result = _compiler.Compile(sourceCode, "TestAssembly");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Assembly.Should().NotBeNull();
        result.AssemblyName.Should().Be("TestAssembly");
        result.LoadedTypes.Should().Contain("TestNamespace.TestClass");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Compile_ShouldFail_WithSyntaxError()
    {
        // Arrange
        var sourceCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        public int Id { get; set; }
        // Missing closing brace
}";

        // Act
        var result = _compiler.Compile(sourceCode, "TestAssembly");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Assembly.Should().BeNull();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Code.Contains("CS"));
    }

    [Fact]
    public void Compile_ShouldFail_WithMissingReference()
    {
        // Arrange
        var sourceCode = @"
using NonExistentNamespace;

namespace TestNamespace
{
    public class TestClass
    {
        public SomeNonExistentType Property { get; set; }
    }
}";

        // Act
        var result = _compiler.Compile(sourceCode, "TestAssembly");

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Compile_ShouldLoadMultipleTypes()
    {
        // Arrange
        var sourceCode = @"
namespace TestNamespace
{
    public class Class1
    {
        public int Id { get; set; }
    }

    public class Class2
    {
        public string Name { get; set; } = string.Empty;
    }

    public interface ITest
    {
        void DoSomething();
    }
}";

        // Act
        var result = _compiler.Compile(sourceCode, "TestAssembly");

        // Assert
        result.Success.Should().BeTrue();
        result.LoadedTypes.Should().HaveCountGreaterOrEqualTo(3);
        result.LoadedTypes.Should().Contain("TestNamespace.Class1");
        result.LoadedTypes.Should().Contain("TestNamespace.Class2");
        result.LoadedTypes.Should().Contain("TestNamespace.ITest");
    }

    [Fact]
    public void Compile_ShouldCompileEntityClassWithDataAnnotations()
    {
        // Arrange
        var sourceCode = @"
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestNamespace
{
    [Table(""Products"")]
    public class Product
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = ""decimal(10,2)"")]
        public decimal Price { get; set; }
    }
}";

        // Act
        var result = _compiler.Compile(sourceCode, "ProductAssembly");

        // Assert
        result.Success.Should().BeTrue();
        result.Assembly.Should().NotBeNull();

        var productType = result.Assembly!.GetType("TestNamespace.Product");
        productType.Should().NotBeNull();
        productType!.GetProperty("Id").Should().NotBeNull();
        productType.GetProperty("Name").Should().NotBeNull();
        productType.GetProperty("Price").Should().NotBeNull();
    }

    [Fact]
    public void CompileMultiple_ShouldSucceed_WithMultipleSourceFiles()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Class1.cs"] = @"
namespace TestNamespace
{
    public class Class1
    {
        public int Id { get; set; }
    }
}",
            ["Class2.cs"] = @"
namespace TestNamespace
{
    public class Class2
    {
        public string Name { get; set; } = string.Empty;
    }
}",
            ["Class3.cs"] = @"
namespace TestNamespace
{
    public class Class3
    {
        public Class1 Reference { get; set; } = null!;
    }
}"
        };

        // Act
        var result = _compiler.CompileMultiple(sources, "MultiFileAssembly");

        // Assert
        result.Success.Should().BeTrue();
        result.LoadedTypes.Should().Contain("TestNamespace.Class1");
        result.LoadedTypes.Should().Contain("TestNamespace.Class2");
        result.LoadedTypes.Should().Contain("TestNamespace.Class3");
    }

    [Fact]
    public void CompileMultiple_ShouldFail_IfAnyFileHasError()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["Class1.cs"] = @"
namespace TestNamespace
{
    public class Class1
    {
        public int Id { get; set; }
    }
}",
            ["Class2.cs"] = @"
namespace TestNamespace
{
    public class Class2
    {
        public int Id { get; set; }
        // Missing closing brace
}"
        };

        // Act
        var result = _compiler.CompileMultiple(sources, "MultiFileAssembly");

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void CompileMultiple_ShouldIncludeFilePath_InErrors()
    {
        // Arrange
        var sources = new Dictionary<string, string>
        {
            ["GoodFile.cs"] = @"
namespace TestNamespace
{
    public class GoodClass
    {
        public int Id { get; set; }
    }
}",
            ["BadFile.cs"] = @"
namespace TestNamespace
{
    public class BadClass
    {
        public UndefinedType Property { get; set; }
    }
}"
        };

        // Act
        var result = _compiler.CompileMultiple(sources, "MultiFileAssembly");

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.FilePath == "BadFile.cs");
    }

    [Fact]
    public void ValidateSyntax_ShouldReturnValid_ForCorrectSyntax()
    {
        // Arrange
        var sourceCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var result = _compiler.ValidateSyntax(sourceCode);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSyntax_ShouldReturnInvalid_ForSyntaxError()
    {
        // Arrange
        var sourceCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        public int Id { get; set; }
        // Missing closing brace
}";

        // Act
        var result = _compiler.ValidateSyntax(sourceCode);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => !string.IsNullOrEmpty(e.Code));
    }

    [Fact]
    public void ValidateSyntax_ShouldNotCheckSemanticErrors()
    {
        // Arrange - This has semantic error (undefined type) but correct syntax
        var sourceCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        public UndefinedType Property { get; set; }
    }
}";

        // Act
        var result = _compiler.ValidateSyntax(sourceCode);

        // Assert
        // ValidateSyntax only checks syntax, not semantics
        // So this should be valid even though UndefinedType doesn't exist
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldProvideErrorLineAndColumn()
    {
        // Arrange
        var sourceCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        public int Id { get; set;
        // Missing closing brace for property
    }
}";

        // Act
        var result = _compiler.Compile(sourceCode, "TestAssembly");

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Line.Should().BeGreaterThan(0);
        result.Errors[0].Column.Should().BeGreaterThan(0);
        result.Errors[0].Message.Should().NotBeEmpty();
    }

    [Fact]
    public void Compile_ShouldHandleComplexEntity()
    {
        // Arrange - Full entity with all features
        var sourceCode = @"
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BobCrm.Domain.Custom
{
    [Table(""Products"")]
    public class Product
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = ""decimal(10,2)"")]
        public decimal Price { get; set; }

        [Required]
        public int Stock { get; set; } = 0;

        [Column(TypeName = ""text"")]
        public string? Description { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? UpdatedBy { get; set; }

        [Required]
        public int Version { get; set; } = 1;
    }
}";

        // Act
        var result = _compiler.Compile(sourceCode, "ProductAssembly");

        // Assert
        result.Success.Should().BeTrue();
        result.Assembly.Should().NotBeNull();
        result.LoadedTypes.Should().Contain("BobCrm.Domain.Custom.Product");

        var productType = result.Assembly!.GetType("BobCrm.Domain.Custom.Product");
        productType.Should().NotBeNull();
        productType!.GetProperties().Should().HaveCount(10);
    }
}
