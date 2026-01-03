/*using Cysharp.Threading.Tasks;
using Nox.Entities;
using Nox.Players;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.Events;

namespace Nox.Sessions {
	public interface ISession {
		/// <summary>
		/// Get the id of the session.
		/// </summary>
		/// <returns></returns>
		public ushort GetId();

		/// <summary>
		/// Get the adapter of the session.
		/// </summary>
		/// <returns></returns>
		public IAdapter GetAdapter();

		/// <summary>
		/// Make the session as the current session.
		/// </summary>
		public UniTask SetCurrent();

		/// <summary>
		/// Check if the session is the current session.
		/// </summary>
		/// <returns></returns>
		public bool IsCurrent();

		/// <summary>
		/// Get player by id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public IPlayer GetPlayer(int id);

		/// <summary>
		/// Get the entity by id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public IEntity GetEntity(int id);

		/// <summary>
		/// Get number of entities in the session.
		/// </summary>
		/// <returns></returns>
		public int GetEntityCount();

		/// <summary>
		/// Get the player by index.
		/// </summary>
		/// <returns></returns>
		public int GetPlayerCount();

		/// <summary>
		/// Close the session.
		/// </summary>
		/// <returns></returns>
		public UniTask Dispose();

		/// <summary>
		/// Notify the session that a player has joined.
		/// </summary>
		/// <param name="player"></param>
		void OnPlayerJoined(IPlayer player);

		/// <summary>
		/// Notify the session that a player has left.
		/// </summary>
		/// <param name="player"></param>
		void OnPlayerLeft(IPlayer player);

		/// <summary>
		/// Notify the session that a player has been selected as the current session.
		/// </summary>
		/// <param name="player"></param>
		void OnAuthorityTransferred(IPlayer player);

		/// <summary>
		/// Notify the session that an entity has been registered.
		/// </summary>
		/// <param name="entity"></param>
		void OnEntityRegistered(IEntity entity);

		/// <summary>
		/// Notify the session that an entity has been unregistered.
		/// </summary>
		/// <param name="entity"></param>
		void OnEntityUnregistered(IEntity entity);

		/// <summary>
		/// Notify the session that an event has been triggered.
		/// </summary>
		/// <param name="event"></param>
		/// <param name="raw"></param>
		/// <param name="sender"></param>
		void OnEvent(string @event, byte[] raw, IPlayer sender);

		/// <summary>
		/// Check if the session matches a specific world identifier.
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		bool Match(IWorldIdentifier identifier);

		/// <summary>
		/// Notify the session that its state has changed.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="previousState"></param>
		void OnStateChanged(IAdapterState state, IAdapterState previousState);

		/// <summary>
		/// Called every frame.
		/// You need call <see cref="IAdapter.OnUpdate"/> method to ensure the adapter is updated.
		/// </summary>
		void OnUpdate();

		/// <summary>
		/// Called when the session is selected.
		/// Please ensure that you call <see cref="IAdapter.OnSelect"/>.
		/// </summary>
		/// <param name="oSession"></param>
		/// <returns></returns>
		UniTask OnSelect(ISession oSession);

		/// <summary>
		/// Called when the session is deselected.
		/// Please ensure that you call <see cref="IAdapter.OnDeselect"/>.
		/// </summary>
		/// <param name="nSession"></param>
		/// <returns></returns>
		UniTask OnDeselect(ISession nSession);

		/// <summary>
		/// Event called when a new world descriptor is added to the session.
		/// </summary>
		/// <param name="descriptor"></param>
		/// <param name="index"></param>
		/// <param name="anchor"></param>
		public void OnSceneLoaded(IWorldDescriptor descriptor, int index, GameObject anchor);

		/// <summary>
		/// Event called when a world descriptor is removed from the session.
		/// </summary>
		/// <param name="index"></param>
		public void OnSceneUnloaded(int index);

		/// <summary>
		/// Get the dimension of the session.
		/// </summary>
		/// <returns></returns>
		public IDimension GetDimension();
	}
}*/