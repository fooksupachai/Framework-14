namespace Boilerplate.AspNetCore
{
    using Boilerplate.AspNetCore.Filters;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.AspNetCore.Rewrite;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// To improve Search Engine Optimization SEO, there should only be a single URL for each resource. Case
    /// differences and/or URL's with/without trailing slashes are treated as different URL's by search engines. This
    /// filter redirects all non-canonical URL's based on the settings specified to their canonical equivalent.
    /// Note: Non-canonical URL's are not generated by this site template, it is usually external sites which are
    /// linking to your site but have changed the URL case or added/removed trailing slashes.
    /// (See Google's comments at http://googlewebmastercentral.blogspot.co.uk/2010/04/to-slash-or-not-to-slash.html
    /// and Bing's at http://blogs.bing.com/webmaster/2012/01/26/moving-content-think-301-not-relcanonical).
    /// </summary>
    public class RedirectToCanonicalUrlRule : IRule
    {
        private const char SlashCharacter = '/';
        private RouteOptions routeOptions;

        /// <summary>
        /// Applies the rule.
        /// Implementations of ApplyRule should set the value for <see cref="RewriteContext.Result" />
        /// (defaults to RuleResult.ContinueRules)
        /// </summary>
        /// <param name="context">The rewrite context.</param>
        public void ApplyRule(RewriteContext context)
        {
            if (HttpMethods.IsGet(context.HttpContext.Request.Method))
            {
                if (!this.TryGetCanonicalUrl(context, out string canonicalUrl))
                {
                    this.HandleNonCanonicalRequest(context, canonicalUrl);
                }
            }
        }

        /// <summary>
        /// Gets the route options.
        /// </summary>
        /// <param name="context">The rewrite context.</param>
        /// <returns>The route options.</returns>
        protected RouteOptions GetRouteOptions(RewriteContext context)
        {
            if (this.routeOptions == null)
            {
                this.routeOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<RouteOptions>>().Value;
            }

            return this.routeOptions;
        }

        /// <summary>
        /// Determines whether the specified URL is canonical and if it is not, outputs the canonical URL.
        /// </summary>
        /// <param name="context">The <see cref="RewriteContext" />.</param>
        /// <param name="canonicalUrl">The canonical URL.</param>
        /// <returns><c>true</c> if the URL is canonical, otherwise <c>false</c>.</returns>
        protected virtual bool TryGetCanonicalUrl(RewriteContext context, out string canonicalUrl)
        {
            var isCanonical = true;

            var request = context.HttpContext.Request;
            var hasPath = request.Path.HasValue && (request.Path.Value.Length > 1);
            var routeOptions = this.GetRouteOptions(context);

            // If we are not dealing with the home page. Note, the home page is a special case and it doesn't matter
            // if there is a trailing slash or not. Both will be treated as the same by search engines.
            if (hasPath)
            {
                var hasTrailingSlash = request.Path.Value[request.Path.Value.Length - 1] == SlashCharacter;

                if (routeOptions.AppendTrailingSlash)
                {
                    // Append a trailing slash to the end of the URL.
                    if (!hasTrailingSlash && !this.HasAttribute<NoTrailingSlashAttribute>(context))
                    {
                        request.Path = new PathString(request.Path.Value + SlashCharacter);
                        isCanonical = false;
                    }
                }
                else
                {
                    // Trim a trailing slash from the end of the URL.
                    if (hasTrailingSlash)
                    {
                        request.Path = new PathString(request.Path.Value.TrimEnd(SlashCharacter));
                        isCanonical = false;
                    }
                }
            }

            if (hasPath || request.QueryString.HasValue)
            {
                if (routeOptions.LowercaseUrls && !this.HasAttribute<NoTrailingSlashAttribute>(context))
                {
                    foreach (var character in request.Path.Value)
                    {
                        if (char.IsUpper(character))
                        {
                            request.Path = new PathString(request.Path.Value.ToLower());
                            isCanonical = false;
                            break;
                        }
                    }

                    if (request.QueryString.HasValue && !this.HasAttribute<NoLowercaseQueryStringAttribute>(context))
                    {
                        foreach (var character in request.QueryString.Value)
                        {
                            if (char.IsUpper(character))
                            {
                                request.QueryString = new QueryString(request.QueryString.Value.ToLower());
                                isCanonical = false;
                                break;
                            }
                        }
                    }
                }
            }

            if (isCanonical)
            {
                canonicalUrl = null;
            }
            else
            {
                canonicalUrl = UriHelper.GetEncodedUrl(request);
            }

            return isCanonical;
        }

        /// <summary>
        /// Handles HTTP requests for URL's that are not canonical. Performs a 301 Permanent Redirect to the canonical URL.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext" />.</param>
        /// <param name="canonicalUrl">The canonical URL.</param>
        protected virtual void HandleNonCanonicalRequest(RewriteContext context, string canonicalUrl)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status301MovedPermanently;
            response.Headers[HeaderNames.Location] = canonicalUrl;
            context.Result = RuleResult.EndResponse;
        }

        /// <summary>
        /// Determines whether the specified action or its controller has the attribute with the specified type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the attribute.</typeparam>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext" />.</param>
        /// <returns><c>true</c> if a <typeparamref name="T"/> attribute is specified, otherwise <c>false</c>.</returns>
        protected virtual bool HasAttribute<T>(RewriteContext context)
        {
            // foreach (var filterMetadata in context.Filters)
            // {
            //     if (filterMetadata is T)
            //     {
            //         return true;
            //     }
            // }
            return false;
        }
    }
}