﻿using System;
using System.CodeDom;
using System.Linq;

using System.Diagnostics;
using Fonlow.TypeScriptCodeDom;
using Fonlow.Reflection;
using Fonlow.Web.Meta;

namespace Fonlow.CodeDom.Web.Ts
{
	/// <summary>
	/// Generate a client function upon ApiDescription for jQuery
	/// </summary>
	public class ClientApiTsFunctionGen : ClientApiTsFunctionGenBase
	{

		readonly bool handleHttpRequestHeaders;
		readonly string contentType;

		public ClientApiTsFunctionGen(string contentType, bool handleHttpRequestHeaders) : base()
		{
			this.contentType = String.IsNullOrEmpty(contentType) ? "application/json;charset=UTF-8" : contentType;
			this.handleHttpRequestHeaders = handleHttpRequestHeaders;
		}

		protected override CodeMemberMethod CreateMethodName()
		{
			return new CodeMemberMethod()
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
				Name = MethodName,
			};
		}

		protected override void RenderImplementation()
		{
			var httpMethodName = Description.HttpMethod.ToLower(); //Method is always uppercase.
			//deal with parameters
			var parameters = Description.ParameterDescriptions.Select(d =>
				 new CodeParameterDeclarationExpression(Poco2TsGen.TranslateToClientTypeReference(d.ParameterDescriptor.ParameterType), d.Name)
			).ToList();

			var returnTypeReference = Poco2TsGen.TranslateToClientTypeReference(ReturnType);
			if (returnTypeReference.BaseType == "response")//response is for NG2 with better built-in support for typing, and get translated to HttpResponse<Blob>
			{
				returnTypeReference.BaseType = "any";
			}

			var callbackTypeText = String.Format("(data : {0}) => any", TypeMapper.MapCodeTypeReferenceToTsText(returnTypeReference));
			Debug.WriteLine("callback: " + callbackTypeText);
			var callbackTypeReference = new CodeSnipetTypeReference(callbackTypeText);
			parameters.Add(new CodeParameterDeclarationExpression(callbackTypeReference, "callback"));

			Method.Parameters.AddRange(parameters.ToArray());

			if (handleHttpRequestHeaders)
			{
				Method.Parameters.Add(new CodeParameterDeclarationExpression(
					"() => {[header: string]: string}", "headersHandler?"));
			}

			var jsUriQuery = UriQueryHelper.CreateUriQueryForTs(Description.RelativePath, Description.ParameterDescriptions);
			var hasArrayJoin = jsUriQuery != null && jsUriQuery.Contains(".join(");
			var uriText = jsUriQuery == null ? $"this.baseUri + '{Description.RelativePath}'" :
				RemoveTrialEmptyString(hasArrayJoin ? $"this.baseUri + '{jsUriQuery}" : $"this.baseUri + '{jsUriQuery}'");

			string headerHandlerCall = handleHttpRequestHeaders ? ", headersHandler" : String.Empty;

			if (httpMethodName == "get" || httpMethodName == "delete")
			{
				Method.Statements.Add(new CodeSnippetStatement($"this.httpClient.{httpMethodName}({uriText}, callback, this.error, this.statusCode{headerHandlerCall});"));
			}
			else if (httpMethodName == "post" || httpMethodName == "put" || httpMethodName == "patch")
			{
				var fromBodyParameterDescriptions = Description.ParameterDescriptions.Where(d => d.ParameterDescriptor.ParameterBinder == ParameterBinder.FromBody
					|| (TypeHelper.IsComplexType(d.ParameterDescriptor.ParameterType) && (!(d.ParameterDescriptor.ParameterBinder == ParameterBinder.FromUri)
					|| (d.ParameterDescriptor.ParameterBinder == ParameterBinder.None)))).ToArray();
				if (fromBodyParameterDescriptions.Length > 1)
				{
					throw new InvalidOperationException(String.Format("This API function {0} has more than 1 FromBody bindings in parameters", Description.ActionDescriptor.ActionName));
				}
				var singleFromBodyParameterDescription = fromBodyParameterDescriptions.FirstOrDefault();

				var dataToPost = singleFromBodyParameterDescription == null ? "null" : singleFromBodyParameterDescription.ParameterDescriptor.ParameterName;

				if (dataToPost == "null")
				{
					Method.Statements.Add(new CodeSnippetStatement($"this.httpClient.{httpMethodName}({uriText}, null, callback, this.error, this.statusCode, '{contentType}'{headerHandlerCall});"));
				}
				else
				{
					Method.Statements.Add(new CodeSnippetStatement($"this.httpClient.{httpMethodName}({uriText}, {dataToPost}, callback, this.error, this.statusCode, '{contentType}'{headerHandlerCall});"));
				}

			}
			else
			{
				Debug.Assert(false, $"How come with {httpMethodName}?");
			}
		}
	}

}
