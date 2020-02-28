﻿using System;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Readers.Exceptions;
using Microsoft.OpenApi;
using System.IO;
using System.Text;
using Fonlow.Web.Meta;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Fonlow.WebApiClientGen.Swag
{
	public class SwaggerToMeta
	{
		public SwaggerToMeta(OpenApiDocument swagDoc, Settings settings)
		{
			this.swagDoc = swagDoc;
			this.settings = settings;

			nameComposer = new NameComposer(settings);
		}

		OpenApiDocument swagDoc;
		Settings settings;
		NameComposer nameComposer;

		public WebApiDescription[] GetDescriptions()
		{
			List<WebApiDescription> ds = new List<WebApiDescription>();
			foreach (var p in swagDoc.Paths)
			{
				var urlPath = p.Key;
				var pathItem = p.Value;
				foreach (var opKV in pathItem.Operations)
				{
					var id = Guid.NewGuid().ToString();
					var d = new WebApiDescription(id);

					var controllerName = nameComposer.GetControllerName(opKV.Value, urlPath);
					var actionDescriptor = new ActionDescriptor();
					d.ActionDescriptor = actionDescriptor;
					actionDescriptor.ControllerDescriptor = new ControllerDescriptor()
					{
						ControllerName = controllerName
					};
					if (opKV.Key > OperationType.Delete)
					{
						throw new InvalidDataException("Support only GET, PUT, POST and DELETE, not " + opKV.Key);
					}

					d.HttpMethod = opKV.Key.ToString();
					d.RelativePath = urlPath;
					actionDescriptor.ActionName = nameComposer.GetActionName(opKV.Value, opKV.Key.ToString());
				}
			}
			return swagDoc.Paths.Select(p =>
			{
				var d = new WebApiDescription(p.Key);
				d.ActionDescriptor.ControllerDescriptor = new ControllerDescriptor()
				{
					ControllerName = nameComposer.PathToControllerName(p.Key)
				};

				d.RelativePath = p.Key;
				d.Documentation = p.Value.Description;
				//var op = p.Value.Operations[OperationType.]

				return d;
			}).ToArray();
		}

	}

	public class NameComposer
	{
		public NameComposer(Settings settings)
		{
			this.settings = settings;
		}

		Settings settings;

		public string PathToControllerName(string path)
		{
			return "";//todo: regex stuffs.
		}

		public string GetActionName(OpenApiOperation op, string httpMethod)
		{
			switch (settings.ActionNameStrategy)
			{
				case ActionNameStrategy.Default:
					return String.IsNullOrEmpty(op.OperationId) ? ComposeActionName(op, httpMethod) : op.OperationId;
				case ActionNameStrategy.OperationId:
					return op.OperationId;
				case ActionNameStrategy.MethodQueryParameters:
					return ComposeActionName(op, httpMethod);
				default:
					throw new InvalidDataException("Impossible");
			}
		}

		public string ComposeActionName(OpenApiOperation op, string httpMethod)
		{
			var byWhat = String.Join("And", op.Parameters.Select(p => ToTitleCase(p.Name)));
			return op.Tags[0].Name + httpMethod + (String.IsNullOrEmpty(byWhat) ? String.Empty : "By" + byWhat);
		}

		public string GetControllerName(OpenApiOperation op, string path)
		{
			switch (settings.ControllerNameStrategy)
			{
				case ControllerNameStrategy.Path:
					return PathToControllerName(path);
				case ControllerNameStrategy.Tags:
					return op.Tags[0].Name;//todo: concanate multiple ones?
				default:
					return "Misc";
			}
		}

		static string ToTitleCase(string s)
		{
			return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
		}

	}

	public enum ActionNameStrategy
	{
		/// <summary>
		/// OperationId or auto
		/// </summary>
		Default,
		OperationId,

		/// <summary>
		/// like GetSomeWhereById1AndId2
		/// </summary>
		MethodQueryParameters,
	}

	public enum ControllerNameStrategy
	{
		/// <summary>
		/// Use tags
		/// </summary>
		Tags,

		/// <summary>
		/// Use path along with regex to pick 
		/// </summary>
		Path,
	}

	public class Settings
	{
		public string ClientNamespace { get; set; }

		public string PathToGroupNameRegex { get; set; }

		public ActionNameStrategy ActionNameStrategy { get; set; }

		public ControllerNameStrategy ControllerNameStrategy { get; set; }
	}
}