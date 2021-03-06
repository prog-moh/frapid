IF OBJECT_ID('auth.save_user_menu_policy') IS NOT NULL
DROP PROCEDURE auth.save_user_menu_policy;

GO


CREATE PROCEDURE auth.save_user_menu_policy
(
    @user_id                        integer,
    @office_id                      integer,
    @allowed_menu_ids               national character varying(MAX),
    @disallowed_menu_ids            national character varying(MAX)
)
AS
BEGIN
    SET NOCOUNT ON;

	DECLARE @allowed_menus			TABLE(menu_id integer);
	DECLARE @disallowed_menus		TABLE(menu_id integer);

	INSERT INTO @allowed_menus
    SELECT  CONVERT(integer, LTRIM(RTRIM(member))) FROM core.split(@allowed_menu_ids);

	INSERT INTO @disallowed_menus
    SELECT  CONVERT(integer, LTRIM(RTRIM(member))) FROM core.split(@disallowed_menu_ids);

    INSERT INTO auth.menu_access_policy(office_id, user_id, menu_id)
    SELECT @office_id, @user_id, core.menus.menu_id
    FROM core.menus
    WHERE core.menus.menu_id NOT IN
    (
        SELECT auth.menu_access_policy.menu_id
        FROM auth.menu_access_policy
        WHERE user_id = @user_id
        AND office_id = @office_id
    );

    UPDATE auth.menu_access_policy
    SET allow_access = NULL, disallow_access = NULL
    WHERE user_id = @user_id
    AND office_id = @office_id;

    UPDATE auth.menu_access_policy
    SET allow_access = 1
    WHERE user_id = @user_id
    AND office_id = @office_id
    AND menu_id IN(SELECT * from @allowed_menus);

    UPDATE auth.menu_access_policy
    SET disallow_access = 1
    WHERE user_id = @user_id
    AND office_id = @office_id
    AND menu_id IN(SELECT * from @disallowed_menus);

    
    RETURN;
END;

GO
