using Bolt.Automation.Common;
using NUnit.Framework;

namespace Bolt.Automation.InfraTests.TestExtension.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AuthorAttribute(Author author) : PropertyAttribute("Author", author.ToString())
    {
        public Author AuthorValue { get; } = author;
    }
}
