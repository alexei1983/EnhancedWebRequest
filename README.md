# Enhanced Web Request
Yet another web API client for .net 8

## Introduction
This package provides basic web API functionality using `HttpClient` with `HttpRequestMessage` and `HttpResponseMessage` in .net 8.

Methods handling entity data in the JSON format use the `System.Text.Json` library.

There are significant changes in version 1.1.1 of the package.

## Example Usage

There are three constructors to the `EnhancedWebRequest` class:

- `public EnhancedWebRequest(HttpClient httpClient)`
- `public EnhancedWebRequest(HttpClientHandler clientHandler)`
- `public EnhancedWebRequest(string baseUrl, EnhancedWebRequestOptions options)`

Assuming we have an existing `HttpClient` object called `httpClient` and an entity of type `MyClass`, we can send a PUT request to update the entity on the remote service using JSON:

```
var webReq = new EnhancedWebRequest(httpClient);
var entity = new MyClass() { Prop = "value", Id = 1234 };
var updatedEntity = await webReq.PutJsonEntity<MyClass>(entity, $"https://api.example.com/rest/entity/{entity.Id}");
```

Methods with an optional `url` parameter handle the value of that parameter as follows:

1. If the URI passed to the method is an absolute URI, it will be used as the request URI, overriding the base URI in the class if one is present.
2. If the URI passed to the method is a relative URI, it will be concatenated with the existing base URI in the class, and the resulting URI will be used as the request URI.
3. If the URI passed to the method is a relative URI and no base URI is present in the class, an exception is thrown.
4. If the URI passed to the method is null or empty string, the base URI in the class will be used as the request URI. If no base URI is present in the class, an exception is thrown.

## Extension Methods

This package provides a number of useful extension methods for working with the `HttpRequestMessage` and `HttpResponseMessage` objects.

In the examples below, assume we already have an instance of the `EnhancedWebRequest` class called `webReq` with a base URI of `https://api.example.com/rest` set.

To de-serialize a response from JSON into a list of objects of type `MyClass`:

```
webReq.NotModified += (obj, e) => 
{
  Debug.WriteLine($"Content at {e.Url} has not been modified.");
};

var response = await webReq.GetIfModifiedSince(DateTime.Now.AddMinutes(-30), "/entities");

if (response.HasContent() && response.IsContentType(MediaTypeNames.Application.Json)) 
{
  var entities = await response.AsJsonEntitiesAsync<MyClass>();
  // Do something with entities
}
```

To handle errors (non-success status codes) received from a remote HTTP endpoint:

```
webReq.ErrorStatusCode += (obj, e) => 
{
  Debug.WriteLine($"Error making API call: {e.StatusCode} was returned.");
  if (e.ResponseMessage.IsContentType("application/problem+json"))
  {
    var problemJson = e.ResponseMessage.AsJsonEntityError<MyProblemClass>();
    // Do something with the error
  }
};

var entity = new MyClass() { Prop = "value", Id = 1234 };
var response = await webReq.PutJsonEntityIfMatch(entity, "e34ab3daa2f", false, /entity");

if (response.HasContent()) 
{
  var newEntity = await response.AsJsonEntityAsync<MyClass>();
  // Do something with the updated entity
}
```

## Issues

There are definitely more robust libraries out there for interacting with web APIs. This package is still in its infancy.