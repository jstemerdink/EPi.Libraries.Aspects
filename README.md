# PostSharp Aspects or use with EPiServer 7. 

By Jeroen Stemerdink

## About

With PostSharp you can simplify your code by moving frequently used actions into aspects.

## Requirements

* EPiServer 7.7+
* .Net 4.0
* PostSharp 4

## Deploy

* Add a reference to the project, or the compiled dll. Also add a reference to PostSharp
* Add the attributes for the desired aspect to your method, class or assembly.
* Build the solution.

## Deploy

CI does not play nice with ‘Enable NuGet Package Restore’. I have not included the downloaded packages, only the packages.config. 
So if you are downoading the project and want to build it locally, you will need to enable ‘Enable NuGet Package Restore’ yourself.
