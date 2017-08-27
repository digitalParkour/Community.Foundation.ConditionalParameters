Sitecore - Personalize Rendering Parameters
==============

Summary
--------------
Sitecore's conditional renderings allow you to override the datasource and/or the component. This extension allows you to override the rendering parameters too.

Sitecore provides a rule to replace the entire parameter query string, however among other reasons it can be tedious to list all parameters to only modify one.
So additionally this extension adds three new conditions to handle common use cases
* set [parameter] to [value]
* set multilist [parameter], append [term]
* set multilist [parameter], remove [term] (if exists)

However, these require the user to know the name and value of the parameters. So, it is recommended to extend these rules and eliminate this knowledge requirement. As an example, you could add a dropdown list with preconfigured values instead of text input.

Setup
--------------
Either:
* Install Sitecore package: Community-Foundation-ConditionalParameters.SitecorePackage.zip
	> This was compiled against Sitecore 8.2 update 5 (170728) NuGet package references.
Or:
1. Include this project in your Helix style solution
2. Update NuGet references to match target sitecore version
3. Sync unicorn data or install sitecore package
4. Consider extending rule actions to ease input for users
