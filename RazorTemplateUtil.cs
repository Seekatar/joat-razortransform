using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine;

namespace RazorTransform
{
    public class RazorTemplateUtil
    {
        /// <summary>
        /// static constructor.
        /// </summary>
        static RazorTemplateUtil()
        {
           
            RazorEngine.Razor.SetTemplateService(new TemplateService(
                new RazorEngine.Configuration.TemplateServiceConfiguration
                    {
                        BaseTemplateType = typeof(XmlTemplate<>),
                        Debug = true,

                    }
                ));
        }

        /// <summary>
        /// Convert a collection of compile errors into a friendly string.
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static string BuildTemplateError(IEnumerable<CompilerError> errors, string source = null)
        {
            StringBuilder b = new StringBuilder();
            if (!String.IsNullOrEmpty(source))
            {
                b.AppendLine(source);
            }
            foreach (var item in errors)
            {
                b.AppendLine(item.ToString());
            }
            return b.ToString();
        }

        /// <summary>
        ///Given a template and a model transforms the template to a text string
        /// </summary>
        /// <param name="model"></param>
        /// <param name="template"></param>
        /// <param name="cacheName"></param>
        /// <returns></returns>
        public static string Transform(object model, string template, string cacheName = null)
        {
            return Transform<object>(model, template,cacheName);
        }
        /// <summary>
        ///Given a template and a model transforms the template to a text string
        /// </summary>
        /// <param name="model"></param>
        /// <param name="template"></param>
        /// <param name="cacheName"></param>
        /// <returns></returns>
        public static string Transform<TModel>(TModel model, string template, string cacheName = null )
        {
            return RazorEngine.Razor.Parse(template, model, cacheName);

        }

        /// <summary>
        ///Given a template and a model transforms the template to a text string.  Catches any exceptions and returns them as a string.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="template"></param>
        /// <param name="cacheName"></param>
        /// <returns></returns>
        public static string TryTransform(object model, string template, out string errorMessage, string cacheName = null)
        {
            return TryTransform<object>(model, template, out errorMessage, cacheName);
        }
        /// <summary>
        ///Given a template and a model transforms the template to a text string.  Catches any exceptions and returns them as a string.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="template"></param>
        /// <param name="cacheName"></param>
        /// <returns></returns>
        public static string TryTransform<TModel>(TModel model, string template, out string errorMessage, string cacheName = null )
        {
            try
            {
                errorMessage = null;

                return RazorEngine.Razor.Parse(template, model,  cacheName);
            }
            catch (RazorEngine.Templating.TemplateCompilationException t)
            {

                errorMessage = BuildTemplateError(t.Errors, t.SourceCode);
                return null;
            }
            catch (Exception ex)
            {

                errorMessage = ex.Message;
                return null;
            }

        }

        /// <summary>
        /// Loads a resource from an assembly as a string
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static string LoadEmebeddedTemplate(string templateName, Assembly assembly = null)
        {
            var resourceStream = assembly != null ? assembly.GetManifestResourceStream(templateName) :
                                                  Assembly.GetExecutingAssembly().GetManifestResourceStream(templateName);

            if (resourceStream == null)
                throw new ArgumentException(String.Format("Specified resource {0} does not exist.", templateName));


            using (StreamReader r = new StreamReader(resourceStream))
            {
                return r.ReadToEnd();
            }
        }

    }

    /// <summary>
    /// This class attempts to relax the Razor2 conditional attribute implementation.  Razor was built to generate HTML and we are
    /// using it to generate XML.  
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XmlTemplate<T> : RazorEngine.Templating.TemplateBase<T>
    {

        /// <summary>
        /// Changes the type of a boolean to a string
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <param name="values"></param>
        public override void WriteAttributeTo(System.IO.TextWriter writer, string name, PositionTagged<string> prefix, PositionTagged<string> suffix, params AttributeValue[] values)
        {
            if (values.Any())
            {
                var first = values.FirstOrDefault();
                if (first != null)
                {
                    if (first.Value.Value == null)
                    {
                        AttributeValue av = new AttributeValue(prefix, new PositionTagged<object>(String.Empty, first.Value.Position), false);
                        base.WriteAttributeTo(writer, name, prefix, suffix, av);
                        return;
                    }
                    else if (first.Value.Value.GetType().Equals(typeof(Boolean)))
                    {
                        AttributeValue av = new AttributeValue(prefix, new PositionTagged<object>(first.Value.Value.ToString(), first.Value.Position), false);
                        base.WriteAttributeTo(writer, name, prefix, suffix, av);
                        return;
                    }
                }
            }
            base.WriteAttributeTo(writer, name, prefix, suffix, values);

        }
    }

}
