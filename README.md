# eScapeLLC.UWP.Charts
Yet another chart component.  Also a play on the venerable "yacc" parser-generator tool.  Yes, I've used it, and know all about LR(1) parsers!

From *Package Manager Console*:
```
   PM> Install-Package eScapeLLC.UWP.Charts
```

[Package page on nuget.org](https://www.nuget.org/packages/eScapeLLC.UWP.Charts/)

# Before you build
If you are rebuilding the application project (e.g. because you forked it), you must re-generate the PFX file, because it is excluded from repository by `.gitignore`.

* In *Solution Explorer*, double-click the app manifest file.
* In the *Manifest Designer*, go to the *Packaging* tab.
* Click on *Choose Certificate*.
* Follow the instructions to make a new test certificate.
* Build away!

# More Info
Please check out the Wiki, it is expanding all the time!