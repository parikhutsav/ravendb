//-----------------------------------------------------------------------
// <copyright file="IIndexingStorageActions.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;
using Raven.Abstractions.Data;
using Raven.Database.Data;
using Raven.Database.Indexing;

namespace Raven.Database.Storage
{
	public interface IIndexingStorageActions : IDisposable
	{
		IEnumerable<IndexStats> GetIndexesStats();

		IndexStats GetIndexStats(string index);
		void AddIndex(string name, bool createMapReduce);
		void DeleteIndex(string name, CancellationToken token);

	    void SetIndexPriority(string name, IndexingPriority priority);


		IndexFailureInformation GetFailureRate(string index);

		void UpdateLastIndexed(string index, Etag etag, DateTime timestamp);
		void UpdateLastReduced(string index, Etag etag, DateTime timestamp);
		void TouchIndexEtag(string index);
		void UpdateIndexingStats(string index, IndexingWorkStats stats);
		void UpdateReduceStats(string index, IndexingWorkStats stats);

		void RemoveAllDocumentReferencesFrom(string key);
		void UpdateDocumentReferences(string view, string key, HashSet<string> references);
		IEnumerable<string> GetDocumentsReferencing(string key);
		int GetCountOfDocumentsReferencing(string key);
		IEnumerable<string> GetDocumentsReferencesFrom(string key);
	}
}
