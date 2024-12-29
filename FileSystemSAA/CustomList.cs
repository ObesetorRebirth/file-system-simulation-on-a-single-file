namespace FileSystemSAA
{
    class CustomList<T>
    {
        private T[] items;
        private int count;

        public CustomList()
        {
            items = new T[10]; 
            count = 0;
        }

        public void Add(T item)
        {
            if (count == items.Length)
            {
                Array.Resize(ref items, items.Length * 2);
            }

            items[count] = item;
            count++;
        }
        public void Remove(T item)
        {
            int index = Array.IndexOf(items, item, 0, count);
            if (index != -1)
            {
                for (int i = index; i < count - 1; i++)
                {
                    items[i] = items[i + 1];
                }

                count--;
            }
        }
        public T[] ToArray()
        {
            T[] result = new T[count];
            Array.Copy(items, result, count);
            return result;
        }
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                {
                    throw new IndexOutOfRangeException();
                }

                return items[index];
            }
            set
            {
                if (index < 0 || index >= count)
                {
                    throw new IndexOutOfRangeException();
                }

                items[index] = value;
            }
        }
    }

}

