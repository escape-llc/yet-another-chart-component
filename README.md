# eScapeLLC.UWP.Charts
Yet another chart component.  Also a play on the venerable "yacc" parser-generator tool.  Yes, I've used it, and know all about LR(1) parsers!

From *Package Manager Console*:
```
   PM> Install-Package eScapeLLC.UWP.Charts
```

[Package page on nuget.org](https://www.nuget.org/packages/eScapeLLC.UWP.Charts/)

# Before you build
If you are rebuilding the application and/or test projects (e.g. because you forked or cloned it), you must re-generate the PFX file, because it is excluded from repository by `.gitignore`.

* In *Solution Explorer*, double-click the app manifest file.
* In the *Manifest Designer*, go to the *Packaging* tab.
* Click on *Choose Certificate*.
* Follow the instructions to make a new test certificate.
* Build away!

## Screen Shot

This is the current demo chart in the solution:
![yacc demo screen shot](http://escape-technology-llc.com/mobile/wp-content/gallery/main/yacc-chart-demo-7.png)

# More Info
Please check out the Wiki, it is expanding all the time!

# Nuget Releases

Please read the wiki pages to learn more about the guts of YACC and rendering and linear algebra in general!

Please consult the other features of this github repository to find out the current state of affairs, *or to contribute*.

## 1.0.0
Initial release.

## 1.1.0
Some breaking changes, sorry about that!

* `PathStyle` et al is the biggest one.
   * make sure to apply a `Style` to your chart components, or you may get "invisible"!
   * see the demo chart XAML for details.
* `HorizontalBand` new decoration.  Tracks two non-series values with a fill in between two horizontal rules.
   * see the demo chart XAML for details.
* `MarkerSeries` now uses a normalized coordinate system [0..1] for the marker geometry.
   * current the marker must be a `Geometry`.
   * marker center is (.5,.5).
   * see the...
* Major fixes to the linear algebra for the graphics.
* Lots of internal fixes/improvements you won't notice.