# Processing Configuration

Settings related to processing maps.

````yaml
yaml:
  [[[DirectoryPoolMaxSize|#directorypoolmaxsize]]]: <int>
  [[[TargetDirectory|#targetdirectory]]]: <string>
  [[[ProcessQueueMaxSize|#processqueuemaxsize]]]: <int>
  [[[JunkFilePatterns|#junkfilepatterns]]]: <string[]>
````

## Configuration Options
### DirectoryPoolMaxSize
`<int> default=3`

The maximum size of the process directory pool.
This means that no more than the given amount of directories will be created
and it in turn dictates the maximum amount of processes that can run in parallel.

### TargetDirectory
`<string>`

This is the target directory for creating the process directory pool.

### ProcessQueueMaxSize
`<int> default=6`

The maximum amount of processes that can be queued up before new process requests will be rejected.

### JunkFilePatterns
`<string[]>`

Glob patterns for files that should be deleted from build directories when getting cleaned.

<seealso>
    <!--Provide links to related how-to guides, overviews, and tutorials.-->
</seealso>