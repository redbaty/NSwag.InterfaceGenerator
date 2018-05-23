# NSwag.InterfaceGenerator

## Introduction

NSwagger helps out a lot when creating rest clients using the almost universal swagger specification, but it lacks a really important feature: It does not create and use interfaces to send stuff to the server, and what this means is that you don't have "Full access" to your model, like attributes and such cause especially in development we re-create these clients a lot so then I came up with a solution and this is it!

## Code Samples

You've got two ways of getting started, you can just use it to generate your rest client or you could use it inside your toolchain, and here's both of them:

### Using demo project
You can use the demo project to generate your classes, just type in:

```
dotnet run -u http://YOURSWAGGERURL
```

All the other instructions are printed out to the console, so just follow them trough.

Note: You can type ``` dotnet run -h ``` to get all possible arguments.


### Using the library
You can see the demo code for some samples to get started on, but mostly all you need is just test it out, it has a FluentApi system too, so you really can't go wrong with it.
