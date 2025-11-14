namespace BobCrm.Api.Domain.Aggregates;

/// <summary>
/// 聚合根值对象的抽象基类
/// AggVO = 主实体VO + 多个子实体VO列表
/// </summary>
public abstract class AggBaseVO
{
    /// <summary>
    /// 获取主实体（Head）的类型
    /// </summary>
    public abstract Type GetHeadEntityType();

    /// <summary>
    /// 获取所有子实体的类型列表
    /// </summary>
    public abstract List<Type> GetSubEntityTypes();

    /// <summary>
    /// 获取主实体VO
    /// </summary>
    public abstract object GetHeadVO();

    /// <summary>
    /// 根据实体类型获取对应的子实体列表
    /// </summary>
    /// <param name="entityType">子实体类型</param>
    /// <returns>子实体列表，如果不存在则返回null</returns>
    public virtual List<object>? GetSubEntities(Type entityType)
    {
        // 遍历所有属性，查找匹配的List类型属性
        var properties = this.GetType().GetProperties();

        foreach (var prop in properties)
        {
            // 检查是否是泛型List
            if (prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // 获取List的内部类型
                var innerType = prop.PropertyType.GetGenericArguments()[0];

                // 如果内部类型匹配
                if (innerType == entityType || innerType.IsAssignableFrom(entityType))
                {
                    var value = prop.GetValue(this);
                    if (value is System.Collections.IList list)
                    {
                        var result = new List<object>();
                        foreach (var item in list)
                        {
                            if (item != null)
                            {
                                result.Add(item);
                            }
                        }
                        return result;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 设置主实体VO
    /// </summary>
    /// <param name="headVO">主实体VO</param>
    public abstract void SetHeadVO(object headVO);

    /// <summary>
    /// 设置子实体列表
    /// </summary>
    /// <param name="entityType">子实体类型</param>
    /// <param name="entities">子实体列表</param>
    public virtual void SetSubEntities(Type entityType, List<object> entities)
    {
        var properties = this.GetType().GetProperties();

        foreach (var prop in properties)
        {
            if (prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var innerType = prop.PropertyType.GetGenericArguments()[0];

                if (innerType == entityType || innerType.IsAssignableFrom(entityType))
                {
                    // 创建强类型List
                    var listType = typeof(List<>).MakeGenericType(innerType);
                    var list = Activator.CreateInstance(listType) as System.Collections.IList;

                    if (list != null)
                    {
                        foreach (var entity in entities)
                        {
                            list.Add(entity);
                        }
                        prop.SetValue(this, list);
                    }
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 保存聚合（主实体 + 所有子实体）
    /// 由具体实现类提供保存逻辑
    /// </summary>
    public abstract Task<int> SaveAsync();

    /// <summary>
    /// 加载聚合（主实体 + 所有子实体）
    /// </summary>
    /// <param name="id">主实体ID</param>
    public abstract Task LoadAsync(int id);

    /// <summary>
    /// 删除聚合（主实体 + 所有子实体）
    /// </summary>
    public abstract Task DeleteAsync();

    /// <summary>
    /// 获取主实体ID
    /// </summary>
    public virtual int GetHeadId()
    {
        var headVO = GetHeadVO();
        if (headVO == null) return 0;

        var idProp = headVO.GetType().GetProperty("Id");
        if (idProp != null)
        {
            var value = idProp.GetValue(headVO);
            if (value != null)
            {
                return Convert.ToInt32(value);
            }
        }

        return 0;
    }

    /// <summary>
    /// 验证聚合数据的有效性
    /// </summary>
    public virtual List<string> Validate()
    {
        var errors = new List<string>();

        // 验证主实体不能为空
        var headVO = GetHeadVO();
        if (headVO == null)
        {
            errors.Add("Head entity cannot be null");
        }

        return errors;
    }

    /// <summary>
    /// 获取所有子实体的总数
    /// </summary>
    public virtual int GetTotalSubEntityCount()
    {
        int count = 0;
        var subTypes = GetSubEntityTypes();

        foreach (var type in subTypes)
        {
            var entities = GetSubEntities(type);
            if (entities != null)
            {
                count += entities.Count;
            }
        }

        return count;
    }

    /// <summary>
    /// 克隆聚合对象（深拷贝）
    /// </summary>
    public virtual AggBaseVO Clone()
    {
        // 使用序列化方式实现深拷贝
        var json = System.Text.Json.JsonSerializer.Serialize(this, this.GetType());
        var cloned = System.Text.Json.JsonSerializer.Deserialize(json, this.GetType());
        return (AggBaseVO)cloned!;
    }
}
