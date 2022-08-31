# AssetLibrary

An editor tool for managing semantic asset labels. Use it to easily manage metadata about your assets, tagging them with any amount of labels that can then be filtered and searched through.

<img width="490" alt="Capture" src="https://user-images.githubusercontent.com/5094696/187627985-e459bdc0-75b3-47cf-b3ff-946c30c92162.PNG">

## How does it work?

Asset Library uses the `userData` field of Unity .metadata files. It should not conflict with any other extension that modifies `userData` as long as those extensions are well behaved. Labels appear in metadata like so:

```
userData: labels={first|second|third}
```

It will create a `ScriptableObject` in `Assets/Resources` in order to store information about labels for quick lookup but the source of truth is the .metadata
