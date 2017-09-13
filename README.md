# eScapeLLC.UWP.Charts
Yet another chart component.  Also a play on the venerable "yacc" parser-generator tool.  Yes, I've used it, and know all about LR(1) parsers!

At some point you can do this:
```
   PM> Install-Package eScapeLLC.UWP.Charts
```

# Before you build
If you are rebuilding the application project (e.g. because you forked it), you must re-generate the PFX file, because it is excluded from repository by *.gitignore*.

* In *Solution Explorer*, double-click the app manifest file.
* In the *Manifest Designer*, go to the *Packaging* tab.
* Click on *Choose Certificate*.
* Follow the instructions to make a new test certificate.
* Build away!

# Packaging
Soon there will be binary assemblies for direct consumption via *nuget.org*.  The rest of this section is general information on publishing packages, and not specific to this project.

## NuGet Command Line
The easiest way to access the NuGet CLI in Visual Studio, is to install the *NuGet.CommandLine* package into the VS Solution, then use the *Package Manager Console*.  Create an account and API key at *nuget.org* if you haven't already done so.

Note that publishing assets are *not* part of the repository or VS Solution!

* In the desired VS project, use "Add|New Folder" to create a *publish* folder.
	* These are automatically excluded from repository by *.gitignore*.
* Right-click the *publish* folder, and select *Exclude From Project* so this folder does not become build output.
* Start the *Package Manager Console*.
	* Select the "Tools|NuGet Package Manager|Package Manager Console" menu item.
	* PMC starts in the *solution folder*.
	* Change directory to the *publish* folder, e.g. "projectname/publish".
	* Run all NuGet CLI commands from this folder.
* Use *nuget spec PackageName* to create the initial *.nuspec* file.
	* The *spec* CLI command creates a "dummy" *.nuspec* file.
	* Edit the file to include metadata and assets you want in the package.
	* File paths are relative to the *publish* folder.
* Use *nuget pack* to create the *.nupkg* package file.
	* Make sure you have previously built the correct solution *configuration* for the assets in the *.nuspec* file, e.g. "Debug|Any CPU".
* Use *nuget push PackageName.x.y.z.nupkg -ApiKey your-api-key-here -Source https://www.nuget.org/api/v2/package* to push the package up to nuget.org.
	* Repeat the build/pack/push sequence for additional versions.