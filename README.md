# Enhanced Web Request
Yet another web API client for .net 8

## Introduction
This package provides basic web API functionality using `HttpClient` with `HttpRequestMessage` and `HttpResponseMessage` in .net 8.

Most methods handling entity data use JSON with the `System.Text.Json` library, except the specific file upload methods (which use content type `multipart/form-data`) 
and the old-style form POST methods (which use content type `application/x-www-form-urlencoded`).

## Example Usage

There are three constructors to the `EnhancedWebRequest` class:

- `public EnhancedWebRequest(HttpClient httpClient)`
- `public EnhancedWebRequest(HttpClientHandler clientHandler)`
- `public EnhancedWebRequest(string baseUrl, EnhancedWebRequestOptions options)`

Assuming we have an existing `HttpClient` object called `httpClient` and an entity of type `MyClass`, we can send a PUT request to update the entity on the remote service:

```
var webReq = new EnhancedWebRequest(httpClient);
var entity = new MyClass() { Prop = "value", Id = 1234 };
var updatedEntity = await webReq.MakeJsonEntityRequest<MyClass>($"https://api.example.com/rest/entity/{entity.Id}", "PUT", entity);
```

If a base URI was set in the `EnhancedWebRequest` object at the time of its creation, any URI passed to a specific request method call will be handled as follows:

1. If the URI passed to the request method is an absolute URI, it will be used in the current request method call as the request URI, overriding the base URI in the class if one
   is set.
2. If the URI passed to the request method is a relative URI, it will be concatenated with the existing base URI of the request object, and the resulting URI will be used in the
   current request method call as the request URI.
3. If the URI passed to the request method is a relative URI and no base URI is set in the request object, an exception is thrown.

## Issues

There are probably more robust libraries out there for interacting with web APIs. This package is still in its infancy.