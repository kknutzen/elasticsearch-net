﻿using System;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Elasticsearch.Net.Serialization;

namespace Nest.Resolvers.Converters
{

	public class BulkRequestConverter : JsonConverter
	{
		public override bool CanRead => false;
		public override bool CanWrite => true;
		public override bool CanConvert(Type objectType) => true;

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var bulk = value as IBulkRequest;
			if (value == null) return;
			var contract = serializer.ContractResolver as SettingsContractResolver;
			var elasticsearchSerializer = contract?.ConnectionSettings.Serializer;
			if (elasticsearchSerializer == null) return ;

			foreach(var op in bulk.Operations)
			{
				op.Index = op.Index ?? bulk.Index ?? op.ClrType;
				if (op.Index.EqualsMarker(bulk.Index)) op.Index = null;
				op.Type = op.Type ?? bulk.Type ?? op.ClrType;
				if (op.Type.EqualsMarker(bulk.Type)) op.Type = null;
				op.Id = op.GetIdForOperation(contract.Infer);

				var opJson = elasticsearchSerializer.SerializeToBytes(op, SerializationFormatting.None);
				writer.WriteRaw($"{{\"{op.Operation}\":" + opJson.Utf8String() + "}\n");
				var body = op.GetBody();
				if (body == null) continue;
				var bodyJson = elasticsearchSerializer.SerializeToBytes(body, SerializationFormatting.None);
				writer.WriteRaw(bodyJson.Utf8String() + "\n");
			}
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotSupportedException();
		}

	}
}