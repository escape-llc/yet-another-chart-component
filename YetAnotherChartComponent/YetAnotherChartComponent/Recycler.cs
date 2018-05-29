﻿using System;
using System.Collections.Generic;

namespace eScapeLLC.UWP.Charts {
	#region RecyclerBase<T>
	/// <summary>
	/// Abstract base for recyclers.
	/// Designed for one-time use; there's no "reset".
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class RecyclerBase<T> {
		#region data
		/// <summary>
		/// Internal list for bookkeeping.
		/// </summary>
		protected readonly List<T> _unused = new List<T>();
		/// <summary>
		/// Internal list for bookkeeping.
		/// </summary>
		protected readonly List<T> _created = new List<T>();
		#endregion
		#region properties
		/// <summary>
		/// Original items that were not used up by iterating.
		/// </summary>
		public IEnumerable<T> Unused { get { return _unused; } }
		/// <summary>
		/// Excess items that were created after original items were used up.
		/// </summary>
		public IEnumerable<T> Created { get { return _created; } }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initializes <see cref="_unused"/> with items.
		/// </summary>
		/// <param name="source">Initial list to reuse; MAY be empty.</param>
		protected RecyclerBase(IEnumerable<T> source) {
			if (source == null) throw new ArgumentNullException(nameof(source));
			_unused.AddRange(source);
		}
		#endregion
	}
	#endregion
	#region Recycler<T>
	/// <summary>
	/// Recycles an input list of instances, then provides new instances after those run out.
	/// Does the bookkeeping to track unused and newly-provided instances.
	/// This implementation has <see cref="IEnumerator{T}"/> semantics.
	/// </summary>
	/// <typeparam name="T">Recycled element type.</typeparam>
	[Obsolete("Use Recycler2<T,S> instead", false)]
	public class Recycler<T> : RecyclerBase<T> {
		#region data
		readonly IEnumerable<T> _source;
		readonly Func<T> _factory;
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="source">Initial list to reuse; MAY be empty.</param>
		/// <param name="factory">Used to create new instances when SOURCE runs out.</param>
		public Recycler(IEnumerable<T> source, Func<T> factory) : base(source) {
#pragma warning disable IDE0016 // Use 'throw' expression
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (factory == null) throw new ArgumentNullException(nameof(factory));
#pragma warning restore IDE0016 // Use 'throw' expression
			_source = source;
			_factory = factory;
		}
		#endregion
		#region public
		/// <summary>
		/// First exhaust the original source, then start creating new instances until caller stops.
		/// Do the bookkeeping for used and created lists.
		/// DO NOT use this to control looping!
		/// </summary>
		/// <returns>Item1: true=created, false=reused; Item2: Another instance.</returns>
		public IEnumerable<Tuple<bool, T>> Items() {
			foreach (var tx in _source) {
				_unused.Remove(tx);
				yield return new Tuple<bool, T>(false, tx);
			}
			while (true) {
				var tx = _factory();
				_created.Add(tx);
				yield return new Tuple<bool, T>(true, tx);
			}
		}
		#endregion
	}
	#endregion
	#region Recycler2<T, S>
	/// <summary>
	/// Recycler that does not use <see cref="System.Collections.IEnumerator"/> to produce instances.
	/// In addition, the <see cref="_factory"/> can receive a state parameter per call to <see cref="Next"/>.
	/// </summary>
	/// <typeparam name="T">Recycled element type.</typeparam>
	/// <typeparam name="S">Factory state type.</typeparam>
	public class Recycler2<T, S> : RecyclerBase<T> {
		#region data
		readonly IEnumerator<T> _source;
		readonly Func<S, T> _factory;
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="source">Initial list to reuse; MAY be empty.</param>
		/// <param name="factory">Used to create new instances when SOURCE runs out.</param>
		public Recycler2(IEnumerable<T> source, Func<S, T> factory) : base(source) {
			if (source == null) throw new ArgumentNullException(nameof(source));
#pragma warning disable IDE0016 // Use 'throw' expression
			if (factory == null) throw new ArgumentNullException(nameof(factory));
#pragma warning restore IDE0016 // Use 'throw' expression
			_source = source.GetEnumerator();
			_factory = factory;
		}
		#endregion
		#region public
		/// <summary>
		/// Return the next item.
		/// </summary>
		/// <param name="state">Some state the factory function can operate with.</param>
		/// <returns>Item1: true=created, false=reused; Item2: Another instance.</returns>
		public Tuple<bool, T> Next(S state) {
			if (_source.MoveNext()) {
				var tx = _source.Current;
				_unused.Remove(tx);
				return new Tuple<bool, T>(false, tx);
			} else {
				var tx = _factory(state);
				_created.Add(tx);
				return new Tuple<bool, T>(true, tx);
			}
		}
		#endregion
	}
	#endregion
}
