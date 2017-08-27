Personalize Rendering Parameters
==============

Summary
--------------
Out of the box Sitecore's conditional rendering rules allows you to override the datasource and/or the component. This extension also allows you to override the rendering parameters too.

Sitecore provides a rule to replace the entire parameter query string, however among other reasons it can be tedious to list all parameters to only modify one.
So additionally this extension adds three new conditions to handle common use cases
* set [parameter] to [value]
* set multilist [parameter], append [term]
* set multilist [parameter], remove [term] (if exists)

These require the user to know then name and value for the parameters. It is recommended to extend these rules to eliminate this knowledge requirement, such as adding dropdown lists with preconfigured values instead of text input.

Setup
--------------

1. Include this project in your Helix style solution
2. Update NuGet references to match target sitecore version
3. Sync unicorn data or install sitecore package
4. Consider extending rule conditions to ease input for users
