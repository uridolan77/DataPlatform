using System.Xml.Linq;
using System.Xml.XPath;

namespace GenericDataPlatform.ETL.Extensions
{
    /// <summary>
    /// Extension methods for XML operations
    /// </summary>
    public static class XmlExtensions
    {
        /// <summary>
        /// Selects an attribute using an XPath expression
        /// </summary>
        public static XAttribute XPathSelectAttribute(this XElement element, string xpath, IXmlNamespaceResolver namespaceResolver = null)
        {
            // Try to select the attribute directly
            var attribute = element.XPathEvaluate(xpath, namespaceResolver) as XAttribute;
            
            if (attribute != null)
            {
                return attribute;
            }
            
            // If the XPath doesn't directly return an attribute, try to select it as an object
            var result = element.XPathEvaluate(xpath, namespaceResolver);
            
            if (result is XAttribute attr)
            {
                return attr;
            }
            
            // If the result is a sequence, try to get the first item as an attribute
            if (result is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is XAttribute xattr)
                    {
                        return xattr;
                    }
                }
            }
            
            return null;
        }
    }
}
