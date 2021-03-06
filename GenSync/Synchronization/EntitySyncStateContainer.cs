﻿// This file is Part of CalDavSynchronizer (http://outlookcaldavsynchronizer.sourceforge.net/)
// Copyright (c) 2015 Gerhard Zehetbauer
// Copyright (c) 2015 Alexander Nimmervoll
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenSync.Synchronization.States;

namespace GenSync.Synchronization
{
  /// <summary>
  /// A container for syncstates, that tracks all created states and disposes them on demand
  /// </summary>
  /// <remarks>
  /// Disposing a SyncState means that the state will  set all references to entities to their default value (which is null for ref types)
  /// This may be helpful, for implementations where the Entity has an underlying COM-Object
  /// </remarks>
  internal class EntitySyncStateContainer<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext> : IDisposable
  {
    private List<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>> _entitySyncStates = new List<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>>();
    private readonly HashSet<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>> _allSyncStatesThatWereCreated = new HashSet<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>>();

    public void Add (IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext> syncState)
    {
      _entitySyncStates.Add (syncState);
      _allSyncStatesThatWereCreated.Add (syncState);
    }

    public void TransformStates (Func<List<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>>, List<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>>> transformation)
    {
      _entitySyncStates = transformation(_entitySyncStates);
      _entitySyncStates.ForEach (s => _allSyncStatesThatWereCreated.Add (s));
    }
    public async Task DoTransition (Func<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>, Task<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>>> transitionFunction)
    {
      List<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>> entitySyncStates = new List<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>>();

      foreach (var state in _entitySyncStates)
      {
        var newState = await transitionFunction (state);
        entitySyncStates.Add (newState);
      }

      _entitySyncStates = entitySyncStates;
      _entitySyncStates.ForEach (s => _allSyncStatesThatWereCreated.Add (s));
    }

    public void DoTransition (Func<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>, IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>> transitionFunction)
    {
      _entitySyncStates = _entitySyncStates.Select (transitionFunction).ToList();
      _entitySyncStates.ForEach (s => _allSyncStatesThatWereCreated.Add (s));
    }

    public void Execute (Action<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>> action)
    {
      _entitySyncStates.ForEach (action);
    }

    public void Dispose ()
    {
      foreach (var syncState in _allSyncStatesThatWereCreated)
        syncState.Dispose();
    }

    public int Count
    {
      get { return _entitySyncStates.Count; }
    }
  }
}