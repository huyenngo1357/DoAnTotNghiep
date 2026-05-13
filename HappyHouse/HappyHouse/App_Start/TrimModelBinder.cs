using System;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace HappyHouse.App_Start
{
    // Trims leading/trailing whitespace for every bound string.
    // Reads unvalidated form values when request validation would throw
    // (so HTML content like from rich editor won't trigger HttpRequestValidationException).
    public class TrimModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            string attempted = null;

            try
            {
                var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
                attempted = valueResult?.AttemptedValue;
            }
            catch (HttpRequestValidationException)
            {
                // If the normal value provider throws because the value contains HTML,
                // read the unvalidated form value directly from the request.
                var request = controllerContext.HttpContext.Request;
                try
                {
                    // Requires .NET 4.5+; returns unvalidated request values.
                    attempted = request.Unvalidated().Form[bindingContext.ModelName];
                }
                catch
                {
                    // Fallback to raw Form collection if Unvalidated not available for any reason.
                    attempted = request.Form[bindingContext.ModelName];
                }
            }

            if (attempted == null)
                return null;

            var trimmed = attempted.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }
}