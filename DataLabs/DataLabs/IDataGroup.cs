using System;
using System.Collections.Generic;

namespace Voidex.DataLabs
{
    public interface IDataGroup
    {
        public string Title { get; set; }
        public Type SourceType { get; set; }
        public List<DataEntity> Content { get; set; }
        public void AddEntity(DataEntity entity);
        public void RemoveEntity(int key);
        public void Sanitize();
    }
}