// QT Include(s)
#include <QDir>

namespace PathUtil {
    static QString combinePath(const QString& path1, const QString& path2) {
        return QDir::cleanPath(path1 + QDir::separator() + path2);
    }

    template <typename... Paths>
    static QString combinePath(const QString& path1, const QString& path2, const Paths&... paths) {
        return combinePath(combinePath(path1, path2), paths...);
    }

    // 拼接两个 HTTP 路径
    QString combineUrlPath(const QString& baseUrl, const QString& relativePath) {
        QString result = baseUrl;

        // 确保 baseUrl 以 '/' 结尾
        if (!result.endsWith('/')) {
            result += '/';
        }

        // 确保 relativePath 不以 '/' 开头
        QString cleanRelativePath = relativePath;
        if (cleanRelativePath.startsWith('/')) {
            cleanRelativePath = cleanRelativePath.mid(1);
        }

        // 拼接路径
        result += cleanRelativePath;

        // 清理多余的 '/'
        return result.replace("//", "/").replace(":/", "://");
    }

    // 可变参数模板：递归拼接多个路径
    template <typename... Paths>
    QString combineUrlPath(const QString& baseUrl, const QString& relativePath, const Paths&... paths) {
        return combineUrlPath(combineUrlPath(baseUrl, relativePath), paths...);
    }
}