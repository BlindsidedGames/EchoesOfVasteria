using System;
using System.IO;
using System.Text;
using Sirenix.Serialization;
using UnityEngine;
using Blindsided.SaveData.Migrations;

namespace Blindsided.SaveData
{
	public static class SaveImportExport
	{
		private const string ExportPrefix = "TE1:";

		public static string ExportCurrentSlot(bool copyToClipboard = false)
		{
			var oracle = Blindsided.Oracle.oracle;
			if (oracle == null || oracle.saveData == null)
				return null;

			// Serialize current in-memory data using Odin Binary
			byte[] binary = SerializationUtility.SerializeValue(oracle.saveData, DataFormat.Binary);
			// Compress to reduce size
			byte[] compressed = Deflate(binary);
			// Encode URL-safe base64 without padding
			string encoded = Base64UrlEncode(compressed);
			string exportString = ExportPrefix + encoded;

			if (copyToClipboard)
				GUIUtility.systemCopyBuffer = exportString;

			return exportString;
		}

		public static bool TryImportToCurrentSlot(string input, out string error)
		{
			error = null;
			if (string.IsNullOrWhiteSpace(input))
			{
				error = "Empty input";
				return false;
			}
			if (!input.StartsWith(ExportPrefix, StringComparison.Ordinal))
			{
				error = "Invalid prefix";
				return false;
			}
			if (input.Length > 1_000_000)
			{
				error = "Input too long";
				return false;
			}

			try
			{
				string payload = input.Substring(ExportPrefix.Length);
				byte[] compressed = Base64UrlDecode(payload);
				byte[] binary = Inflate(compressed);
				var data = SerializationUtility.DeserializeValue<GameData>(binary, DataFormat.Binary);
				if (data == null)
				{
					error = "Failed to decode save";
					return false;
				}

				var oracle = Blindsided.Oracle.oracle;
				if (oracle == null)
				{
					error = "Oracle missing";
					return false;
				}

				// Determine slot and ensure SaveManager is pointed at it
				var index = Mathf.Clamp(oracle.CurrentSlot, 0, 2);
				var slotName = $"Save{index + 1}";
				SaveManager.Instance.SetCurrentSlot(slotName);

				// Run migrations if required (backs up previous snapshot and persists on success)
				bool migrated = SaveMigrationRunner.Run(data, Application.version, slotName);

				// Assign to runtime
				oracle.saveData = data;

				// If no migrations were applied (thus not persisted), save now
				if (!migrated)
				{
					SaveManager.Instance.SaveAsync(oracle.saveData).GetAwaiter().GetResult();
				}

				oracle.PersistSlotMetadataToPlayerPrefs();

				// Refresh runtime state
				Blindsided.EventHandler.ResetData();
				Blindsided.EventHandler.LoadData();
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				Blindsided.Utilities.FeedbackForm.SubmitException(
					"SaveImportExport.Import",
					ex,
					$"inputLength: {input?.Length ?? 0}");
				return false;
			}
		}

		private static byte[] Deflate(byte[] input)
		{
			using (var output = new MemoryStream())
			{
				using (var ds = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
				{
					ds.Write(input, 0, input.Length);
				}
				return output.ToArray();
			}
		}

		private static byte[] Inflate(byte[] input)
		{
			using (var ms = new MemoryStream(input))
			using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress))
			using (var output = new MemoryStream())
			{
				ds.CopyTo(output);
				return output.ToArray();
			}
		}

		private static string Base64UrlEncode(byte[] bytes)
		{
			string s = Convert.ToBase64String(bytes);
			s = s.Replace('+', '-').Replace('/', '_');
			return s.TrimEnd('=');
		}

		private static byte[] Base64UrlDecode(string s)
		{
			s = s.Replace('-', '+').Replace('_', '/');
			s = s.PadRight((s.Length + 3) / 4 * 4, '=');
			return Convert.FromBase64String(s);
		}
	}
}
