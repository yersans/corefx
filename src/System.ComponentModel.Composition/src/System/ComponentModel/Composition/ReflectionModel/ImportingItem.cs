// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.Linq;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal abstract class ImportingItem
    {
        private readonly ContractBasedImportDefinition _definition;
        private readonly ImportType _importType;

        protected ImportingItem(ContractBasedImportDefinition definition, ImportType importType)
        {
            Assumes.NotNull(definition);

            _definition = definition;
            _importType = importType;
        }

        public ContractBasedImportDefinition Definition
        {
            get { return _definition; }
        }

        public ImportType ImportType
        {
            get { return _importType; }
        }

        public object CastExportsToImportType(Export[] exports)
        {
            if (Definition.Cardinality == ImportCardinality.ZeroOrMore)
            {
                return CastExportsToCollectionImportType(exports);
            }
            else
            {
                return CastExportsToSingleImportType(exports);
            }
        }

        private object CastExportsToCollectionImportType(Export[] exports)
        {
            Assumes.NotNull(exports);

            // Element type could be null if the actually import type of the member is not a collection
            // This particular case will end up failing when we set the member.
            Type elementType = ImportType.ElementType ?? typeof(object);

            Array array = Array.CreateInstance(elementType, exports.Length);

            for (int i = 0; i < array.Length; i++)
            {
                object value = CastSingleExportToImportType(elementType, exports[i]);

                array.SetValue(value, i);
            }

            return array;
        }

        private object CastExportsToSingleImportType(Export[] exports)
        {
            Assumes.NotNull(exports);
            Assumes.IsTrue(exports.Length < 2);

            if (exports.Length == 0)
            {   
                return null;
            }

            return CastSingleExportToImportType(ImportType.ActualType, exports[0]);
        }

        private object CastSingleExportToImportType(Type type, Export export)
        {
            if (ImportType.CastExport != null)
            {
                return ImportType.CastExport(export);
            }

            return Cast(type, export);
        }

        private object Cast(Type type, Export export)
        {
            object value = export.Value;

            object result;
            if (!ContractServices.TryCast(type, value, out result))
            {
                throw new ComposablePartException(
                    String.Format(CultureInfo.CurrentCulture,
                        SR.ReflectionModel_ImportNotAssignableFromExport,
                        export.ToElement().DisplayName,
                        type.FullName),
                    Definition.ToElement());
            }

            return result;
        }
    }
}