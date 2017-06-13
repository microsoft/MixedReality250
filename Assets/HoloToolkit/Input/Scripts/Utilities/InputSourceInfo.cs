// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoloToolkit.Unity.InputModule
{
    // TODO: robertes: comment for HoloToolkit release.
    public struct InputSourceInfo
    {
        public IInputSource InputSource;
        public uint SourceId;

        public InputSourceInfo(InputSourceEventArgs e) :
            this()
        {
            InputSource = e.InputSource;
            SourceId = e.SourceId;
        }

        public bool Matches(InputSourceEventArgs e)
        {
            return (e != null)
                && (e.InputSource == InputSource)
                && (e.SourceId == e.SourceId)
                ;
        }
    }
}
