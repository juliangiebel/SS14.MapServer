# Server Configuration



````yaml
yaml:
  [[[Host|#host]]]: <string>
  [[[CorsOrigins|#corsorigins]]]: <string[]>
  [[[Language|#language]]]: <string>
  [[[UseHttps|#usehttps]]]: [true|false]
  [[[UseForwardedHeaders|#useforwardedheaders]]]: [true|false]
  [[[PathBase|#pathbase]]]: <string>
  [[[RateLimitCount|#ratelimitcount]]]: <int>
  [[[RateLimitWindowMinutes|#ratelimitwindowminutes]]]: <long>
  [[[EnableSentry|#enablesentry]]]: [true|false]
  [[[EnableSentryTracing|#enablesentrytracing]]]: [true|false]
````

## Configuration Options
### Host
`<string> default="https://localhost:7154`

The URL the map server is hosted behind.

### CorsOrigins
`<string[]>`

A list of allowed cors origins.

### Language
`<string> default="en-us"`

The locale the map server should use.

### UseHttps
`[true|false] default=false`

Enables https redirection if true. Set this to false if run behind a reverse proxy.

### UseForwardedHeaders
`[true|false] default=true`

Enables support for reverse proxy headers like "X-Forwarded-Host" if true. Set this to true if run behind a reverse proxy.

### PathBase
`<string>`

Sets the request base path used before any routes apply i.e. "/base/api/Maps" with "/base" being the PathBase.
Set this if run behind a reverse proxy on a sub path and the proxy doesn't strip the path the server is hosted on.

> Add a slash before the path: "/path".
{style="note"}

### RateLimitCount
`<int> default = 20`

The amount of requests allowed by per client inside the rate limit window.

### RateLimitWindowMinutes
`<long> default=1`

The amount of time before the rate limit replenishes.
%project-name% is using a fixed window rate limiter.


### EnableSentry
`[true|false] default=false`

Whether the Sentry integration is enabled or not.

> Configure sentry using the `Sentry` configuration section and any configuration options provided by Sentry.
> [https://docs.sentry.io/platforms/dotnet/configuration](https://docs.sentry.io/platforms/dotnet/configuration)

### EnableSentryTracing
`[true|false] default=false`

Enables sentries performance monitoring on endpoints if Sentry is enabled.

<seealso>
    <!--Provide links to related how-to guides, overviews, and tutorials.-->
</seealso>