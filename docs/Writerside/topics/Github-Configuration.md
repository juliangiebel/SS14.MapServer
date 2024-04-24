# GitHub Configuration

The GitHub configuration section contains settings required to set up integration with GitHub.

````yaml
yaml:
  [[[Enabled|#enabled]]]: [true|false]
  [[[AppName|#appname]]]: <string>
  [[[AppPrivateKeyLocation|#appprivatekeylocation]]]: <string>
  [[[AppId|#appid]]]: <int>
  [[[TemplateLocation|#templatelocation]]]: <string>
````

## Configuration Options
### Enabled
`[true|false] default=true`

Enables GitHub integration. If this is set to false Posting on PRs and checking changed files doesn't work.
Setting this to disabled requires GitHub Webhooks to be set up if you still want automatic map rendering.

### AppName
`<string>`

The name of the GitHub app.

### AppPrivateKeyLocation
`<string>`

The path to the private key file created when setting up the GitHubApp-

### AppId
`<int>`

The app id of the GitHub app

### TemplateLocation
`<string>`

The location of liquid templates used for GitHub PR comments.

<seealso>
    <!--Provide links to related how-to guides, overviews, and tutorials.-->
</seealso>